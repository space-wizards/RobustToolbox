using Robust.Shared.Configuration;

namespace Robust.Shared.Utility;

internal static class VersionInformationPrinter
{
    public static string[] GetInformationDump(IConfigurationManager cfg)
    {
        var buildInfo = GameBuildInformation.GetBuildInfoFromConfig(cfg);

        return new[]
        {
            $"Fork ID: {buildInfo.ForkId}",
            $"Version: {buildInfo.Version}",
        };
    }
}
