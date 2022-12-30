namespace Robust.Shared.Configuration;

internal static class ConfigHelpers
{
    public static int GetEffectiveMaxConnections(this IConfigurationManager cfg)
    {
#pragma warning disable CS0618
        var maxPlayers = cfg.GetCVar(CVars.GameMaxPlayers);
#pragma warning restore CS0618
        if (maxPlayers != 0)
            return maxPlayers;

        return cfg.GetCVar(CVars.NetMaxConnections);
    }
}
