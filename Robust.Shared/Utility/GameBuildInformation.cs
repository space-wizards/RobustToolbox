using Robust.Shared.Configuration;

namespace Robust.Shared.Utility;

internal sealed record GameBuildInformation(
    string EngineVersion,
    string? ZipHash,
    string? ZipDownload,
    string ForkId,
    string Version,
    string? ManifestHash,
    string? ManifestUrl,
    string? ManifestDownloadUrl
)
{
    public static GameBuildInformation GetBuildInfoFromConfig(IConfigurationManager cfg)
    {
        var zipHash = cfg.GetCVar(CVars.BuildHash);
        var manifestHash = cfg.GetCVar(CVars.BuildManifestHash);
        var forkId = cfg.GetCVar(CVars.BuildForkId);
        var forkVersion = cfg.GetCVar(CVars.BuildVersion);

        var manifestDownloadUrl = Interpolate(cfg.GetCVar(CVars.BuildManifestDownloadUrl));
        var manifestUrl = Interpolate(cfg.GetCVar(CVars.BuildManifestUrl));
        var zipDownload = Interpolate(cfg.GetCVar(CVars.BuildDownloadUrl));

        if (zipDownload == "")
            zipDownload = null;

        if (zipHash == "")
            zipHash = null;

        if (manifestHash == "")
            manifestHash = null;

        if (manifestDownloadUrl == "")
            manifestDownloadUrl = null;

        if (manifestUrl == "")
            manifestUrl = null;

        return new GameBuildInformation(
            cfg.GetCVar(CVars.BuildEngineVersion),
            zipHash,
            zipDownload,
            forkId,
            forkVersion,
            manifestHash,
            manifestUrl,
            manifestDownloadUrl
        );

        string? Interpolate(string? value)
        {
            // Can't tell if splitting the ?. like this is more cursed than
            // failing to align due to putting the full ?. on the next line
            return value?
                .Replace("{FORK_VERSION}", forkVersion)
                .Replace("{FORK_ID}", forkId)
                .Replace("{MANIFEST_HASH}", manifestHash)
                .Replace("{ZIP_HASH}", zipHash);
        }
    }
}
