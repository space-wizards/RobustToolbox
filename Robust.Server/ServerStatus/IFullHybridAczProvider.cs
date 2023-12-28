using System.Threading;
using System.Threading.Tasks;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;

namespace Robust.Server.ServerStatus;

/// <summary>
/// Provides additional game files for use on top of Hybrid ACZ.
/// </summary>
/// <seealso href="https://docs.spacestation14.com/en/robust-toolbox/acz.html"/>
public interface IFullHybridAczProvider
{
    /// <summary>
    /// Run Full Hybrid ACZ packaging.
    /// This function is expected to calculate the asset graph with the provided passes and inject its own files.
    /// After the function finishes, Hybrid ACZ files are injected which should finish the packaging process.
    /// </summary>
    /// <param name="hybridPackageInput">
    /// The asset pass that will receive the files from the Hybrid ACZ package.
    /// You probably want to pass these through to the output directly.
    /// </param>
    /// <param name="output">
    /// The final output pass that all packaged game files should be sent to.
    /// </param>
    /// <param name="logger">A logger for logging of log messages you may want to log.</param>
    /// <param name="cancel">Cancellation token to abort packaging if necessary.</param>
    /// <returns>An asynchronous task.</returns>
    Task Package(AssetPass hybridPackageInput, AssetPass output, IPackageLogger logger, CancellationToken cancel);
}
