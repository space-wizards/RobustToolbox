using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Robust.Shared;

/// <summary>
/// Contains some preset CVar overrides applied in the engine to aid development.
/// </summary>
internal static class CVarDefaultOverrides
{
    public static void OverrideClient(IConfigurationManager cfg)
    {
        OverrideShared(cfg);

#if FULL_RELEASE
        return;
#endif

        // Profiling is currently only useful on the client, so only enable it there.
        cfg.OverrideDefault(CVars.ProfEnabled, true);
    }

    public static void OverrideServer(IConfigurationManager cfg)
    {
        OverrideShared(cfg);

#if FULL_RELEASE
        return;
#endif

        // Set auth to optional in case you're doing any funny development shenanigans.
        cfg.OverrideDefault(CVars.AuthMode, (int) AuthMode.Optional);
    }

    private static void OverrideShared(IConfigurationManager cfg)
    {
#if FULL_RELEASE
        return;
#endif

        // Increase default profiler memory use on local builds to make it more useful.
        cfg.OverrideDefault(CVars.ProfBufferSize, 65536);
        cfg.OverrideDefault(CVars.ProfIndexSize, 1024);
    }
}
