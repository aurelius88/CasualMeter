using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.DamageMeter;

namespace CasualMeter.Common.Conductors.Messages
{
    public class PastePlayerStatsMessage
    {
        public Func<IEnumerable<PlayerInfo>, IEnumerable<PlayerInfo>> Modification { get; set; }
        public string PreHeading { get; set; }
        public string Heading { get; set; }
        public string Format { get; set; }
    }
}
