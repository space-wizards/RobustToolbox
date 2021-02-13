using System;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

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
