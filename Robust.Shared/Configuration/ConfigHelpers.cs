using System;

namespace Robust.Shared.Configuration;

internal static class ConfigHelpers
{
    public static int GetEffectiveMaxConnections(this IConfigurationManager cfg)
    {
#pragma warning disable CS0618
        return Math.Max(cfg.GetCVar(CVars.NetMaxConnections), cfg.GetCVar(CVars.GameMaxPlayers));
#pragma warning restore CS0618
    }
}
