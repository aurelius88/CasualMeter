// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using Lunyx.Common.UI.Wpf;
using Nicenis.ComponentModel;
using Tera.DamageMeter.Annotations;

namespace Tera.DamageMeter
{
    public class SkillStats : PropertyObservable
    {
        private readonly DamageTracker _tracker;
        private readonly ThreadSafeObservableCollection<SkillResult> _skillLog;

        private bool IsPartyStats => _tracker == null || _skillLog == null;

        public SkillStats() : this(null, null) { }

        /// <summary>
        /// Creates a new instance of SkillStats
        /// </summary>
        /// <param name="tracker">DamageTracker used for calculations</param>
        /// <param name="skillLog">SkillLog used for calculations</param>
        public SkillStats(DamageTracker tracker, ThreadSafeObservableCollection<SkillResult> skillLog)
        {
            _tracker = tracker;
            _skillLog = skillLog;
        }

        public void UpdateStats()
        {
            if (IsPartyStats) return;

            //update stats
            long totalDamage = Dealt ? _tracker.TotalDealt.Damage : _tracker.TotalReceived.Damage;
            DamageFraction = totalDamage == 0 ? 1 : (double)Damage / totalDamage;
            Dps = _tracker.CalculateDps(Damage);

            //update personal DPS
            var firstOrDefault = _skillLog.FirstOrDefault(s => _tracker.IsValidAttack(s));
            var lastOrDefault = _skillLog.LastOrDefault(s => _tracker.IsValidAttack(s));
            PersonalDps = (firstOrDefault != null && lastOrDefault != null)
                ? _tracker.CalculateDps(Damage, lastOrDefault.Time - firstOrDefault.Time)
                : _tracker.CalculateDps(Damage, TimeSpan.Zero);
        }

        public bool Dealt
        {
            get { return GetProperty<bool>(getDefault: () => true); }
            set { SetProperty(value); }
        }

        public long Damage
        {
            get { return GetProperty<long>(getDefault: () => 0); }
            set { SetProperty(value, onChanged: e => UpdateStats()); }
        }

        public long Heal
        {
            get { return GetProperty<long>(getDefault: () => 0); }
            set { SetProperty(value); }
        }

        public int Hits
        {
            get { return GetProperty<int>(getDefault: () => 0); }
            set { SetProperty(value, onChanged: e => CritFraction = (double)Crits/Hits); }
        }

        public int Crits
        {
            get { return GetProperty<int>(getDefault: () => 0); }
            set { SetProperty(value); }
        }

        public double CritFraction
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }

        public double DamageFraction
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }

        public long Dps
        {
            get { return GetProperty<long>(); }
            set { SetProperty(value); }
        }

        public long PersonalDps
        {
            get { return GetProperty<long>(); }
            set { SetProperty(value); }
        }

        public void Add(SkillStats other)
        {
            Damage += other.Damage;
            Heal += other.Heal;
            Crits += other.Crits;
            Hits += other.Hits;
        }
    }
}