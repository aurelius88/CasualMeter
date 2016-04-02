using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tera.Game.Messages;

namespace Tera.DamageMeter
{
    public static class MessageExtensions
    {
        public static bool IsValid(this EachSkillResultServerMessage message, DamageTracker tracker = null)
        {
            return message != null && !message.IsUseless && //stuff like warrior DFA
                   (tracker?.FirstAttack != null || (!message.IsHeal && message.Amount > 0)) &&//only record first hit if it's a damage hit (heals occurring outside of fights)
                   !(message.Target.Equals(message.Source) && !message.IsHeal && message.Amount > 0);//disregard damage dealt to self (gunner self destruct)
        }
    }
}
