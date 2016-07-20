﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tera.DamageMeter;
using Tera.Game.Messages;
using Newtonsoft.Json;
using Tera.Game;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Entities;
using CasualMeter.Common.Helpers;
using Lunyx.Common.UI.Wpf;
using Tera.Data;

namespace CasualMeter.Common.TeraDpsApi
{
    public static class DataExporter
    {
        public static void ToTeraDpsApi(ExportType exportType, DamageTracker damageTracker, TeraData teraData)
        {
            if (exportType == ExportType.None) return;

            //if we want to upload, primary target must be dead
            if (exportType.HasFlag(ExportType.Upload) && !damageTracker.IsPrimaryTargetDead)
                return;

            var exportToExcel = (exportType & (ExportType.Excel | ExportType.ExcelTemp)) != 0;
            //if we're not exporting to excel and credentials aren't fully entered, return
            if (!exportToExcel
                && (string.IsNullOrEmpty(SettingsHelper.Instance.Settings.TeraDpsToken)
                    || string.IsNullOrEmpty(SettingsHelper.Instance.Settings.TeraDpsUser)))
                return;

            //ignore if not a boss
            var entity = damageTracker.PrimaryTarget;
            if (!(entity?.Info.Boss ?? false)) return;

            var abnormals = damageTracker.Abnormals;

            bool timedEncounter = false;
            //Nightmare desolarus
            if (entity.Info.HuntingZoneId == 759 && entity.Info.TemplateId == 1003)
            {
                timedEncounter = true;
            }
            
            var firstHit = damageTracker.StatsByUser.SelectMany(x => x.SkillLog).Where(x => x.Target == entity).Min(x => x.Time as DateTime?) ?? new DateTime(0);
            var lastHit  = damageTracker.StatsByUser.SelectMany(x => x.SkillLog).Where(x => x.Target == entity).Max(x => x.Time as DateTime?) ?? new DateTime(0);
            var firstTick = firstHit.Ticks;
            var lastTick = lastHit.Ticks;
            var interval = lastTick - firstTick;
            var seconds = interval/TimeSpan.TicksPerSecond;
            if (seconds == 0) return;
            long totaldamage;
            if (timedEncounter)
                totaldamage =
                    damageTracker.StatsByUser.SelectMany(x => x.SkillLog)
                        .Where(x => x.Time >= firstHit && x.Time <= lastHit)
                        .Sum(x => x.Damage);
            else
                totaldamage =
                    damageTracker.StatsByUser.SelectMany(x => x.SkillLog)
                        .Where(x => x.Target == entity)
                        .Sum(x => x.Damage);

            var partyDps = TimeSpan.TicksPerSecond * totaldamage / interval;
            var teradpsData = new EncounterBase
            {
                encounterDateTime = damageTracker.FirstAttack?.Date.ToUniversalTime() ?? DateTime.UtcNow,
                areaId = entity.Info.HuntingZoneId + "",
                bossId = entity.Info.TemplateId + "",
                fightDuration = seconds + "",
                partyDps = partyDps + ""
            };

            foreach (var debuff in abnormals.Get(entity))
            {
                long percentage = debuff.Value.Duration(firstTick, lastTick) * 100 / interval;
                if(percentage == 0)
                {
                    continue;
                }
                teradpsData.debuffUptime.Add(new KeyValuePair<string, string>(
                    debuff.Key.Id+"", percentage+""
                    ));
            }

            foreach (var user in damageTracker.StatsByUser)
            {
                var filteredSkillog = timedEncounter
                    ? user.SkillLog.Where(x => x.Time >= firstHit && x.Time <= lastHit).ToList()
                    : user.SkillLog.Where(x => x.Target == entity).ToList();

                long damage=filteredSkillog.Sum(x => x.Damage);
                if (damage <= 0) continue;

                var teradpsUser = new Members();

                teradpsUser.playerTotalDamage = damage + "";
                var buffs = abnormals.Get(user.Player);
                teradpsUser.playerClass = user.Class.ToString();
                teradpsUser.playerName = user.Name;
                teradpsUser.playerServer = SettingsHelper.Instance.BasicTeraData.Servers.GetServerName(user.Player.ServerId);
                teradpsUser.playerAverageCritRate = Math.Round(100 * (double)filteredSkillog.Count(x => x.IsCritical && x.Damage > 0) / filteredSkillog.Count(x => x.Damage > 0), 1) + "";
                teradpsUser.healCrit = user.Player.IsHealer ? Math.Round(100 * (double)filteredSkillog.Count(x => x.IsCritical && x.Heal > 0) / filteredSkillog.Count(x => x.Heal > 0), 1) + "" : null;
                teradpsUser.playerDps = TimeSpan.TicksPerSecond * damage / interval + "";
                teradpsUser.playerTotalDamagePercentage = damage * 100 / totaldamage + "";

                var death = buffs.Death;
                teradpsUser.playerDeaths = death.Count(firstTick, lastTick) + "";
                teradpsUser.playerDeathDuration = death.Duration(firstTick, lastTick)/TimeSpan.TicksPerSecond + "";

                var aggro = buffs.Aggro(entity);
                teradpsUser.aggro = 100 * aggro.Duration(firstTick, lastTick) / interval + "";

                foreach (var buff in buffs.Times)
                {
                    long percentage = (buff.Value.Duration(firstTick, lastTick) * 100 / interval);
                    if (percentage == 0)
                    {
                        continue;
                    }
                    teradpsUser.buffUptime.Add(new KeyValuePair<string, string>(
                        buff.Key.Id + "", percentage + ""
                    ));
                }

                var aggregated = new List<AggregatedSkillResult>();
                var collection = new ThreadSafeObservableCollection<SkillResult>();
                foreach (var skill in filteredSkillog)
                {
                    collection.Add(skill);
                    if (aggregated.All(asr => !skill.IsSameSkillAs(asr)))
                        aggregated.Add(new AggregatedSkillResult(skill.SkillShortName,skill.IsHeal,AggregationType.Name, collection));
                }
                foreach (var skill in aggregated)
                {
                    var skillLog = new SkillLog();
                    var skilldamage = skill.Damage;
                    if (skilldamage == 0) continue;

                    skillLog.skillAverageCrit = skill.AverageCrit + "";
                    skillLog.skillAverageWhite = skill.AverageWhite + "";
                    skillLog.skillCritRate = Math.Round(skill.CritRate * 100, 1) + "";
                    skillLog.skillDamagePercent = Math.Round(skill.DamagePercent * 100, 1) + "";
                    skillLog.skillHighestCrit = skill.HighestCrit + "";
                    skillLog.skillHits = skill.Hits + "";
                    skillLog.skillId = teraData.SkillDatabase.GetSkillByPetName(skill.NpcInfo?.Name,user.Player.RaceGenderClass)?.Id.ToString() ?? skill.SkillId.ToString();
                    skillLog.skillLowestCrit = skill.LowestCrit + "";
                    skillLog.skillTotalDamage = skilldamage + "";

                    teradpsUser.skillLog.Add(skillLog);
                }
                teradpsData.members.Add(teradpsUser);
            }

            //export to excel if specified
            if (exportToExcel) Task.Run(() => ExcelExport.ExcelSave(exportType, teradpsData, teraData));

            //return if we don't need to upload
            if (!exportType.HasFlag(ExportType.Upload)) return;

            /*
              Validation, without that, the server cpu will be burning \o 
            */
            var areaId = int.Parse(teradpsData.areaId);
            if (
                areaId != 886 &&
                areaId != 467 &&
                areaId != 767 &&
                areaId != 768 &&
                areaId != 470 &&
                areaId != 468
                )
            {
                return;
            }

            if (int.Parse(teradpsData.partyDps) < 2000000 && areaId != 468)
            {
                return;
            }
            var json = JsonConvert.SerializeObject(teradpsData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Task.Run(() => Send(entity, json, 3));
        }

        private static void Send(NpcEntity boss, string json, int numberTry)
        {
            var sent = false;
            while (numberTry-- > 0 && !sent)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("X-Auth-Token", SettingsHelper.Instance.Settings.TeraDpsToken);
                        client.DefaultRequestHeaders.Add("X-User-Id", SettingsHelper.Instance.Settings.TeraDpsUser);

                        var response = client.PostAsync("http://teradps.io/api/que", new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json")
                        );
                        sent = true;
                        var responseString = response.Result.Content.ReadAsStringAsync();
                        Debug.WriteLine(responseString.Result);
                        Dictionary<string, object> responseObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString.Result);
                        if (responseObject.ContainsKey("id"))
                        {
                            Debug.WriteLine((string)responseObject["id"] + " " + boss.Info.Name);
                        }
                        else
                        {
                            Debug.WriteLine("!" + (string)responseObject["message"] + " " + boss.Info.Name + " " + DateTime.Now.Ticks);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
