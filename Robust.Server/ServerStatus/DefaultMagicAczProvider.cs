using System.Threading;
using System.Threading.Tasks;
using Robust.Packaging;
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
        IPackageWriter writer,
        CancellationToken cancel)
    {
        var (binFolderPath, assemblyNames) = _info;

        var contentDir = FindContentRootPath(_deps);
        await RobustClientPackaging.WriteContentAssemblies(
            writer,
            contentDir,
            binFolderPath,
            assemblyNames,
            cancel);

        await RobustClientPackaging.WriteClientResources(contentDir, writer, cancel);
    }

    // deps is for future proofing, not currently used.
    public static string FindContentRootPath(IDependencyCollection deps)
    {
        return PathHelpers.ExecutableRelativeFile("../..");
    }
}

public sealed record DefaultMagicAczInfo(string BinFolderPath, string[] AssemblyNames);
