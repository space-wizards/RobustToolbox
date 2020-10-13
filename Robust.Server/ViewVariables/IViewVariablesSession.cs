using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Network;

namespace Robust.Server.ViewVariables
{
    internal interface IViewVariablesSession
    {
        IViewVariablesHost Host { get; }
        IRobustSerializer RobustSerializer { get; }
        NetUserId PlayerUser { get; }
        object Object { get; }
        uint SessionId { get; }
        Type ObjectType { get; }
    }
}
