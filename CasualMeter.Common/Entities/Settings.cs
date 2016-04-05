using CasualMeter.Common.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CasualMeter.Common.Entities
{
    public class Settings : DefaultValueEntity
    {
        [DefaultValue(100)]
        public double WindowLeft { get; set; }

        [DefaultValue(100)]
        public double WindowTop { get; set; }

        [DefaultValue(1)]
        public double Opacity { get; set; }

        [DefaultValue(1)]
        public double UiScale { get; set; }

        [DefaultValue(true)]
        public bool IsPinned { get; set; }

        [DefaultValue(false)]
        public bool UseCompactView { get; set; }

        [DefaultValue(false)]
        public bool ShowPersonalDps { get; set; }

        [DefaultValue(false)]
        public bool ShowHeader { get; set; }

        [DefaultValue("Dps, crit and damage done ({Boss} in {Time}):")]
        public string DpsPreHeader { get; set; }

        [DefaultValue("(Class) Name               | DPS        | Damage in % | Crit in %")]
        public string DpsHeader { get; set; }

        [DefaultValue("{ClassAndName,-26} {DPS,-10} {DamagePercent,-10} Crits: {CritPercent,-10}")]
        public string DpsPasteFormat { get; set; }

        [DefaultValue("Received damage/dps ({Boss} in {Time}):")]
        public string RcvPreHeader { get; set; }

        [DefaultValue("(Class) Name               | Received Dmg | RDPS")]
        public string RcvHeader { get; set; }

        [DefaultValue("{ClassAndName,-26} -{DamageReceived,-11} -{RDPS}")]
        public string RcvPasteFormat { get; set; }

        [DefaultValue("Heal done/received ({Boss} in {Time}):")]
        public string HealPreHeader { get; set; }

        [DefaultValue("(Class) Name               | Heal       | Received Heal")]
        public string HealHeader { get; set; }

        [DefaultValue("{ClassAndName,-26} healed: {Heal,-10} received: {HealReceived,-10}")]
        public string HealPasteFormat { get; set; }

        [DefaultValue(30)]
        public int InactivityResetDuration { get; set; }

        [DefaultValue(5)]
        public int ExpandedViewPlayerLimit { get; set; }

        [DefaultValue(false)]
        public bool UseGlobalHotkeys { get; set; }

        [DefaultValue(false)]
        public bool OnlyBosses { get; set; }

        [DefaultValue(true)]
        public bool IgnoreOneshots { get; set; }

        [DefaultValue(false)]
        public bool AutosaveEncounters { get; set; }

        [DefaultValue(false)]
        public bool UseRawSockets { get; set; }
        
        [JsonConverter(typeof(LanguageConverter))]
        [DefaultValue("Auto")]
        public string Language { get; set; }

        //since you can't set DefaultValueAttribute on objects
        private HotKeySettings _hotkeys;
        public HotKeySettings HotKeys
        {
            get { return _hotkeys ?? (_hotkeys = new HotKeySettings()); }
            set { _hotkeys = value; }
        }
    }
}
