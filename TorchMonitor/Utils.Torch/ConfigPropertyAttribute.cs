using System;
using VRage.Game.ModAPI;

namespace Utils.Torch
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigPropertyAttribute : Attribute
    {
        public ConfigPropertyAttribute(ConfigPropertyType type)
        {
            Type = type;
        }

        public ConfigPropertyType Type { get; }

        public bool IsVisibleTo(MyPromoteLevel promoLevel)
        {
            if (promoLevel == MyPromoteLevel.None &&
                Type != ConfigPropertyType.VisibleToPlayers)
            {
                return false;
            }

            return true;
        }
    }
}