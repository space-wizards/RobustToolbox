using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.Server.ServerStatus;

public sealed class DefaultMagicAczProvider : IMagicAczProvider
{
    private readonly DefaultMagicAczInfo _info;
    private readonly IDependencyCollection _deps;

    public DefaultMagicAczProvider(DefaultMagicAczInfo info, IDependencyCollection deps)
    {
        _info = info;
        _deps = deps;
    }

    public async Task Package(
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var (binFolderPath, assemblyNames) = _info;

        var graph = new RobustClientAssetGraph();
        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        AssetGraph.CalculateGraph(graph.AllPasses.Append(pass).ToArray(), logger);

        var inputPass = graph.Input;

        var contentDir = FindContentRootPath(_deps);
        await RobustClientPackaging.WriteContentAssemblies(
            inputPass,
            contentDir,
            binFolderPath,
            assemblyNames,
            cancel);

        await RobustClientPackaging.WriteClientResources(contentDir, inputPass, cancel);

        inputPass.InjectFinished();
    }

    // deps is for future proofing, not currently used.
    public static string FindContentRootPath(IDependencyCollection deps)
    {
        return PathHelpers.ExecutableRelativeFile("../..");
    }
}

public sealed record DefaultMagicAczInfo(string BinFolderPath, string[] AssemblyNames);
