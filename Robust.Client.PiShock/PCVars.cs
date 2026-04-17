using Robust.Shared.Configuration;

namespace Robust.Client.PiShock;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public static class PCVars
{
    /// <summary>
    /// Enable FUN
    /// </summary>
    public static readonly CVarDef<bool> PiShockEnabled =
        CVarDef.Create("pishock.enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> PiShockUsername =
        CVarDef.Create("pishock.username", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> PiShockApiKey =
        CVarDef.Create("pishock.api_key", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> PiShockShareCode =
        CVarDef.Create("pishock.share_code", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE | CVar.CONFIDENTIAL);

    // Clamped onto every operation as a safety cap.
    public static readonly CVarDef<int> PiShockMaxIntensity =
        CVarDef.Create("pishock.max_intensity", 50, CVar.CLIENTONLY | CVar.ARCHIVE);

    // Clamped onto every operation as a safety cap.
    public static readonly CVarDef<int> PiShockMaxDuration =
        CVarDef.Create("pishock.max_duration", 5, CVar.CLIENTONLY | CVar.ARCHIVE);

    // Minimum seconds between operations. Engine enforces a floor of 1s regardless of this value.
    public static readonly CVarDef<float> PiShockCooldown =
        CVarDef.Create("pishock.cooldown", 2.0f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
