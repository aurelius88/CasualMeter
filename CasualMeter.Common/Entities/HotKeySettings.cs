using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CasualMeter.Common.Entities
{
    public class HotKeySettings : DefaultValueEntity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierPaste { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierReset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierSave { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.F1)]
        public Key PasteDpsStats { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.F2)]
        public Key PasteRcvStats { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.F3)]
        public Key PasteHealStats { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.F5)]
        public Key Reset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.F8)]
        public Key Save { get; set; }
    }
}
