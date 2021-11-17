using System.Threading.Tasks;

namespace Robust.Server.ServerStatus
{
    public delegate bool StatusHostHandler(
        IStatusHandlerContext context);
    public delegate Task<bool> StatusHostHandlerAsync(
        IStatusHandlerContext context);
}
