using System.Threading;
using System.Threading.Tasks;
using Robust.Packaging;

namespace Robust.Server.ServerStatus;

public interface IMagicAczProvider
{
    // Cancellation not currently used, future proofing can't hurt though.
    Task Package(IPackageWriter writer, CancellationToken cancel);
}
