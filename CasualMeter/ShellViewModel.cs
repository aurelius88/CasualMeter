using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Formatters;
using CasualMeter.Common.Helpers;
using CasualMeter.Common.UI.ViewModels;
using GalaSoft.MvvmLight.CommandWpf;
using log4net;
using Lunyx.Common.UI.Wpf;
using NetworkSniffer;
using Tera;
using Tera.DamageMeter;
using Tera.Data;
using Tera.Game;
using Tera.Game.Messages;
using Tera.Sniffing;
using Nicenis.ComponentModel;
using System.Threading;

namespace CasualMeter
{
    public class ShellViewModel : CasualViewModelBase
    {
        private ITeraSniffer _teraSniffer;
        private TeraData _teraData;
        private MessageFactory _messageFactory;
        private EntityTracker _entityTracker;
        private PlayerTracker _playerTracker;

        private Stopwatch _inactivityTimer = new Stopwatch();

        public ShellViewModel()
        {
            CasualMessenger.Instance.Messenger.Register<PastePlayerStatsMessage>(this, PasteStats);
            CasualMessenger.Instance.Messenger.Register<ResetPlayerStatsMessage>(this, ResetDamageTracker);
        }

        #region Properties

        public BasicTeraData BasicTeraData
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.BasicTeraData); }
            set { SetProperty(value); }
        }

        public Server Server
        {
            get { return GetProperty<Server>(); }
            set { SetProperty(value, onChanged: OnServerChanged); }
        }

        public ThreadSafeObservableCollection<DamageTracker> ArchivedDamageTrackers
        {
            get { return GetProperty(getDefault: () => new ThreadSafeObservableCollection<DamageTracker>()); }
            set { SetProperty(value); }
        }

        public DamageTracker DamageTracker
        {
            get { return GetProperty<DamageTracker>(); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    _inactivityTimer.Restart();
                    PlayerCount = value.StatsByUser.Count;
                });
            }
        }
        #endregion

        public bool IsPinned
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.IsPinned); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.IsPinned = value;
                    ProcessHelper.Instance.ForceVisibilityRefresh(true);
                });
            }
        }
        public bool OnlyBosses
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.OnlyBosses); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.OnlyBosses = value;
                    if (DamageTracker != null)
                    {
                        DamageTracker.OnlyBosses = value;
                    }
                });
            }
        }

        public bool IgnoreOneshots
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.IgnoreOneshots); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.IgnoreOneshots = value;
                    if (DamageTracker != null)
                    {
                        DamageTracker.IgnoreOneshots = value;
                    }
                });
            }
        }

        public bool AutosaveEncounters 
        { 
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.AutosaveEncounters); } 
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.AutosaveEncounters = value);} 
        } 

        public bool ShowCompactView => UseCompactView || (SettingsHelper.Instance.Settings.ExpandedViewPlayerLimit > 0 
                                                          && PlayerCount > SettingsHelper.Instance.Settings.ExpandedViewPlayerLimit);

        public int PlayerCount
        {
            get { return GetProperty<int>(); }
            // ReSharper disable once ExplicitCallerInfoArgument
            set { SetProperty(value, onChanged: e => OnPropertyChanged(nameof(ShowCompactView))); }
        }

        public bool UseCompactView
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseCompactView); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.UseCompactView = value;
                    // ReSharper disable once ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(ShowCompactView));
                });
            }
        }

        public bool ShowPersonalDps
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.ShowPersonalDps); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.ShowPersonalDps = value); }
        }

        public bool UseGlobalHotkeys
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseGlobalHotkeys); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.UseGlobalHotkeys = value); }
        }

        public bool UseRawSockets
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseRawSockets); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.UseRawSockets = value;
                    Initialize();
                });
            }
        }

        public bool ShowHeader
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.ShowHeader); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.ShowHeader = value); }
        }

        public string HeaderDamage
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.DpsHeader); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.DpsHeader = value); }
        }

        public void ResetDpsHeader ()
        {
            SettingsHelper.Instance.Settings.DpsHeader = 
                GetProperty(getDefault: () => SettingsHelper.Instance.Settings.DpsHeader);
            
        }

        #region Commands

        public RelayCommand ToggleIsPinnedCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand(ToggleIsPinned)); }
            set { SetProperty(value); }
        }

        public RelayCommand<DamageTracker> LoadEncounterCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand<DamageTracker>(LoadEncounter)); }
            set { SetProperty(value); }
        }

        public RelayCommand ClearEncountersCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand(ClearEncounters, () => ArchivedDamageTrackers.Count > 0)); }
            set { SetProperty(value); }
        }
        #endregion

        private object _snifferLock = new object();

        public void Initialize()
        {
            Task.Factory.StartNew(() =>
            {
                lock (_snifferLock)
                {
                    if (_teraSniffer != null)
                    {   //dereference the existing sniffer if it exists
                        var sniffer = _teraSniffer;
                        _teraSniffer = null;
                        sniffer.Enabled = false;
                        sniffer.MessageReceived -= HandleMessageReceived;
                        sniffer.NewConnection -= HandleNewConnection;
                        Logger.Info("Sniffer has been disabled.");
                    }

                    IpSniffer ipSniffer = null;
                    if (UseRawSockets)
                    {
                        ipSniffer = new IpSnifferRawSocketMultipleInterfaces();
                    }

                    _teraSniffer = new TeraSniffer(ipSniffer, BasicTeraData.Servers);
                    _teraSniffer.MessageReceived += HandleMessageReceived;
                    _teraSniffer.NewConnection += HandleNewConnection;
                    _teraSniffer.Enabled = true;

                    Logger.Info("Sniffer has been enabled.");
                }
            }, TaskCreationOptions.LongRunning);//provide hint to start on new thread
        }

        private void HandleNewConnection(Server server)
        {
            Server = server;
            _teraData = BasicTeraData.DataForRegion(server.Region);
            _entityTracker = new EntityTracker(_teraData.NpcDatabase);
            _playerTracker = new PlayerTracker(_entityTracker,BasicTeraData.Servers);
            _messageFactory = new MessageFactory(_teraData.OpCodeNamer);

            ResetDamageTracker();
            DamageTracker = DamageTracker ?? new DamageTracker
            {
                OnlyBosses = OnlyBosses,
                IgnoreOneshots = IgnoreOneshots
            };

            Logger.Info($"Connected to server {server.Name}.");
        }

        private void OnServerChanged(IPropertyValueChangedEventArgs<Server> valueChanged)
        {
            if (valueChanged.NewValue != null)
            {
                _teraData = BasicTeraData.DataForRegion(valueChanged.NewValue.Region);
                BasicTeraData.Servers.Region = valueChanged.NewValue.Region;
                _entityTracker = new EntityTracker(_teraData.NpcDatabase);
                _playerTracker = new PlayerTracker(_entityTracker);
                _messageFactory = new MessageFactory(_teraData.OpCodeNamer);
                
                ResetDamageTracker();
                DamageTracker = DamageTracker ?? new DamageTracker
                {
                    OnlyBosses = OnlyBosses,
                    IgnoreOneshots = IgnoreOneshots
                };
            }
        }

        private void ResetDamageTracker(ResetPlayerStatsMessage message = null)
        {
            if (Server == null) return;

            bool saveEncounter = message != null && message.ShouldSaveCurrent;
            if (saveEncounter && !DamageTracker.IsArchived && DamageTracker.StatsByUser.Count > 0 && 
                DamageTracker.FirstAttack != null && DamageTracker.LastAttack != null)
            {
                DamageTracker.IsArchived = true;
                ArchivedDamageTrackers.Add(DamageTracker);
                return;
            }
            if (message != null && !message.ShouldSaveCurrent && DamageTracker.IsArchived)
            {
                ArchivedDamageTrackers.Remove(DamageTracker);
            }

            DamageTracker = new DamageTracker
            {
                OnlyBosses = OnlyBosses,
                IgnoreOneshots = IgnoreOneshots
            };
        }

        private void HandleMessageReceived(Message obj)
        {
            var message = _messageFactory.Create(obj);
            _entityTracker.Update(message);

            var despawnNpc = message as SDespawnNpc;
            if (despawnNpc != null && !DamageTracker.IsArchived)
            {
                Entity ent = _entityTracker.GetOrPlaceholder(despawnNpc.NPC);
                if (ent is NpcEntity)
                {
                    var npce = ent as NpcEntity;
                    if (npce.Info.Boss && despawnNpc.Dead)
                    {
                        DamageTracker.Name = npce.Info.Name; //Name encounter with the last dead boss
                        if (AutosaveEncounters) CasualMessenger.Instance.ResetPlayerStats(true);
                    }
                }
                return;
            }
            if (DamageTracker.IsArchived)
            { 
                var npcOccupier = message as SNpcOccupierInfo;
                if (npcOccupier != null)
                {
                    Entity ent = _entityTracker.GetOrPlaceholder(npcOccupier.NPC);
                    if (ent is NpcEntity)
                    {
                        var npce = ent as NpcEntity;
                        if (npce.Info.Boss && npcOccupier.Target != EntityId.Empty) 
                        {
                            CasualMessenger.Instance.ResetPlayerStats(true); //Stop viewing saved encounter on boss aggro
                        }
                    }
                    return;
                }
            }

            var skillResultMessage = message as EachSkillResultServerMessage;
            if (SettingsHelper.Instance.Settings.InactivityResetDuration > 0
                && _inactivityTimer.Elapsed > TimeSpan.FromSeconds(SettingsHelper.Instance.Settings.InactivityResetDuration)
                && skillResultMessage.IsValid())
            {
                CasualMessenger.Instance.ResetPlayerStats(AutosaveEncounters||DamageTracker.IsArchived);
            }
            if (!DamageTracker.IsArchived && skillResultMessage.IsValid(DamageTracker)) //don't process while viewing a past encounter
            {
                var skillResult = new SkillResult(skillResultMessage, _entityTracker, _playerTracker, _teraData.SkillDatabase,_teraData.NpcDatabase);
                DamageTracker.Update(skillResult);
                if (!skillResult.IsHeal && skillResult.Amount > 0)
                    _inactivityTimer.Restart();
                PlayerCount = DamageTracker.StatsByUser.Count;
            }

            var sLogin = message as LoginServerMessage;
            if (sLogin != null)
            {
                Server=BasicTeraData.Servers.GetServer(sLogin.ServerId,Server);
                return;
            }
        }

        private void PasteStats(PastePlayerStatsMessage obj)
        {
            if (DamageTracker == null) return;
            if (obj == null) throw new ArgumentNullException("obj", "There is no message to paste.");

            var playerStatsSequence = obj.Modification(DamageTracker.StatsByUser);
            const int maxLength = 300;

            var sb = new StringBuilder();

            // PREHEADER
            Formatter placeHolder = new DamageTrackerFormatter(DamageTracker, FormatHelpers.Invariant);
            var preHeaderString = placeHolder.Replace(obj.PreHeading);
            sb.AppendLine(preHeaderString);

            var isActive = ProcessHelper.Instance.IsTeraActive;
            if (isActive.HasValue && isActive.Value)
            {
                ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(1);
                ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(150);
                //send text input to Tera
                if (!ProcessHelper.Instance.SendString(preHeaderString))
                    Logger.Warn("Couldn't send text input to Tera. (PreHeader)");
                Thread.Sleep(150);
                ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(1);
                ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(50);
            }

            // HEADER
            sb.AppendLine(obj.Heading);
            if (isActive.HasValue && isActive.Value && SettingsHelper.Instance.Settings.ShowHeader)
            {
                ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(1);
                ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(150);
                //send text input to Tera
                if (!ProcessHelper.Instance.SendString(obj.Heading))
                    Logger.Warn("Couldn't send text input to Tera. (Header)");
                Thread.Sleep(150);
                ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(1);
                ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(50);
            }

            // PLAYER STATS
            Logger.Debug("player stats=" + playerStatsSequence.Count());
            foreach (var playerInfo in playerStatsSequence)
            {
                placeHolder = new PlayerStatsFormatter(playerInfo, FormatHelpers.Invariant);
                string playerText = placeHolder.Replace(obj.Format);

                if (playerText.Length > maxLength)
                {
                    Logger.Warn("Skip player text, because text length="+playerText.Length+ " exceeds max length="+maxLength+".");
                    break;
                }
            
                if (isActive.HasValue && isActive.Value)
                {
                    ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                    Thread.Sleep(1);
                    ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                    Thread.Sleep(150);
                    //send text input to Tera
                    if (!ProcessHelper.Instance.SendString(playerText))
                        Logger.Warn("Couldn't send text input to Tera. (PlayerStat)");

                    Thread.Sleep(150);
                    ProcessHelper.Instance.PressKey(System.Windows.Forms.Keys.Enter);
                    Thread.Sleep(1);
                    ProcessHelper.Instance.ReleaseKey(System.Windows.Forms.Keys.Enter);
                    Thread.Sleep(50);
                }
                sb.AppendLine(playerText);
            }
            if (sb.Length > 0)
            {
                //copy to clipboard in case user wants to paste outside of Tera
                Application.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject(sb.ToString()));
            }
        }

        private void LoadEncounter(DamageTracker obj)
        {
            DamageTracker = obj;
        }

        private void ClearEncounters()
        {
            ArchivedDamageTrackers.Clear();
        }

        private void ToggleIsPinned()
        {
            IsPinned = !IsPinned;
        }
    }
}
