using System;
using SS13_Shared;

namespace ClientInterfaces.GOC
{
    public interface IPlayerAction
    {
        PlayerActionTargetType TargetType { get; }
        string Icon { get; }
        string Name { get; }
        string Description { get; }
        DateTime CooldownExpires { get; set; }
        uint Uid { get; }
        void Activate();
        void Use(object target);
    }
}