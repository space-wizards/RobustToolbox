using System;
using System.Threading.Tasks;

namespace Robust.Server.ServerStatus
{
    [Obsolete("Use async handlers")]
    public delegate bool StatusHostHandler(
        IStatusHandlerContext context);
    public delegate Task<bool> StatusHostHandlerAsync(
        IStatusHandlerContext context);
}
