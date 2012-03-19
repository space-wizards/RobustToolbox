using System;
using System.Collections.Generic;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientInterfaces.GOC
{
    public interface IPlayerAction
    {
        PlayerActionTargetType TargetType { get; }
        void Activate();
        void Use(object target);
        string Icon { get; }
        string Name { get; }
        string Description { get; }
        DateTime CooldownExpires { get; set; }
        uint Uid { get; }
    }
}
