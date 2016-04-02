using System;
using CasualMeter.Common.Conductors.Messages;
using GalaSoft.MvvmLight.Messaging;
using Lunyx.Common.UI.Wpf;
using Tera.DamageMeter;
using Tera.Game;
using CasualMeter.Common.Helpers;
using System.Linq;

namespace CasualMeter.Common.Conductors
{
    public sealed class CasualMessenger
    {
        private static readonly Lazy<CasualMessenger> Lazy = new Lazy<CasualMessenger>(() => new CasualMessenger());

        public static CasualMessenger Instance => Lazy.Value;

        public IMessenger Messenger { get { return GalaSoft.MvvmLight.Messaging.Messenger.Default; } }

        private CasualMessenger()
        {
        }

        public void ResetPlayerStats(bool shouldSaveCurrent)
        {
            Messenger.Send(new ResetPlayerStatsMessage
            {
                ShouldSaveCurrent = shouldSaveCurrent
            });
        }

        public void PasteDpsStats()
        {
            var message = new PastePlayerStatsMessage();
            message.Modification = players =>
                players.OrderByDescending(playerInfo => playerInfo.Dealt.Damage)
                       .TakeWhile(x => x.Dealt.Damage > 0);
            message.PreHeading = SettingsHelper.Instance.Settings.DpsPreHeader;
            message.Heading = SettingsHelper.Instance.Settings.DpsHeader;
            message.Format = SettingsHelper.Instance.Settings.DpsPasteFormat;
            Messenger.Send(message);
        }

        public void PasteReceivedStats()
        {
            var message = new PastePlayerStatsMessage();
            message.Modification = players =>
                players.OrderByDescending(playerInfo => playerInfo.Received.Damage)
                       .TakeWhile(x => x.Received.Damage > 0);
            message.PreHeading = SettingsHelper.Instance.Settings.RcvPreHeader;
            message.Heading = SettingsHelper.Instance.Settings.RcvHeader;
            message.Format = SettingsHelper.Instance.Settings.RcvPasteFormat;
            Messenger.Send(message);
        }

        public void PasteHealStats()
        {
            var message = new PastePlayerStatsMessage();
            message.Modification = players =>
                players.OrderByDescending(playerInfo => playerInfo.Dealt.Heal);
            message.PreHeading = SettingsHelper.Instance.Settings.HealPreHeader;
            message.Heading = SettingsHelper.Instance.Settings.HealHeader;
            message.Format = SettingsHelper.Instance.Settings.HealPasteFormat;
            Messenger.Send(message);
         }

        public void RefreshVisibility(bool? isVisible, bool toggle)
        {
            Messenger.Send(new RefreshVisibilityMessage
            {
                IsVisible = isVisible,
                Toggle = toggle
            });
        }

        public void UpdateSkillBreakdownView(object sender, string viewKey)
        {
            Messenger.Send(new UpdateSkillBreakdownViewMessage
            {
                ViewKey = viewKey
            }, sender);
        }

    }
}
