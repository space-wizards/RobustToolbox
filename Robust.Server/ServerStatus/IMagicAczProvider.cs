using System.Threading;
using System.Threading.Tasks;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;

namespace Robust.Server.ServerStatus;

public interface IMagicAczProvider
{
    // Cancellation not currently used, future proofing can't hurt though.
    Task Package(AssetPass pass, IPackageLogger logger, CancellationToken cancel);
}
