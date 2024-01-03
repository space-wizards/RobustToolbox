using System;
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
#if FULL_RELEASE
        throw new InvalidOperationException("Default Magic ACZ is not available on full release builds. Make sure your server is packaged with Hybrid ACZ or otherwise has build information configured.");
#endif

        var (binFolderPath, assemblyNames) = _info;

        var graph = new RobustClientAssetGraph();
        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        AssetGraph.CalculateGraph(graph.AllPasses.Append(pass).ToArray(), logger);

        var inputPass = graph.Input;

        var contentDir = FindContentRootPath(_deps);
        await RobustSharedPackaging.WriteContentAssemblies(
            inputPass,
            contentDir,
            binFolderPath,
            assemblyNames,
            cancel: cancel);

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
