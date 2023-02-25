using Robust.Shared.Configuration;

namespace Robust.Client.WebView;

// ReSharper disable once InconsistentNaming
/// <summary>
/// CVars for <c>Robust.Client.WebView</c>
/// </summary>
[CVarDefs]
public static class WCVars
{
    /// <summary>
    /// Enable the <c>res://</c> protocol inside WebView browsers, allowing access to the Robust resources.
    /// </summary>
    public static readonly CVarDef<bool> WebResProtocol =
        CVarDef.Create("web.res_protocol", true, CVar.CLIENTONLY);

    /// <summary>
    /// Overrides the default CEF user-agent when set to a non-empty string.
    /// </summary>
    public static readonly CVarDef<string> WebUserAgentOverride =
        CVarDef.Create("web.user_agent", "", CVar.CLIENTONLY);

    /// <summary>
    /// If true, use headless WebView implementation even with graphical client (turn off CEF).
    /// </summary>
    public static readonly CVarDef<bool> WebHeadless =
        CVarDef.Create("web.headless", false, CVar.CLIENTONLY);
}
