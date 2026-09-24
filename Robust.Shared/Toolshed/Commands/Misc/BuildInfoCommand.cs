using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed partial class BuildInfoCommand : ToolshedCommand
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly string Gold = Color.Gold.ToHex();

    [CommandImplementation]
    public void BuildInfo(IInvocationContext ctx)
    {
        var game = _cfg.GetCVar(CVars.BuildForkId);
        var buildCommit = _cfg.GetCVar(CVars.BuildHash);
        var buildManifest = _cfg.GetCVar(CVars.BuildManifestHash);
        var engine = _cfg.GetCVar(CVars.BuildEngineVersion);

        ctx.WriteLine(FormattedMessage.FromMarkupOrThrow($"""
            [color={Gold}]Game:[/color] {game}
            [color={Gold}]Build commit:[/color] {buildCommit}
            [color={Gold}]Manifest hash:[/color] {buildManifest}
            [color={Gold}]Engine ver:[/color] {engine}
            """));
    }
}
