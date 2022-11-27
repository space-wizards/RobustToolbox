using System.Text.Json.Nodes;

namespace Robust.Server.ServerStatus;

/// <summary>
/// Helper functions for dealing with <see cref="StatusHost"/>.
/// </summary>
public static class StatusHostHelpers
{
    /// <summary>
    /// Add an info link to the body of an info request.
    /// </summary>
    /// <param name="infoObject">
    /// The main body of the info request.
    /// This should be the value from the parameter on <see cref="IStatusHost.OnInfoRequest"/>.
    /// </param>
    /// <param name="name">The name of the link to add.</param>
    /// <param name="url">The URL to open when the link is clicked.</param>
    /// <param name="icon">
    /// The icon to show next to the button.
    /// See https://docs.spacestation14.io/config-reference for valid icons.
    /// </param>
    public static void AddLink(JsonNode infoObject, string name, string url, string? icon=null)
    {
        var infoObjectCast = (JsonObject)infoObject;
        if (!infoObjectCast.TryGetPropertyValue("links", out var linksArray))
        {
            linksArray = new JsonArray();
            infoObjectCast.Add("links", linksArray);
        }

        var arr = (JsonArray)linksArray!;
        arr.Add(new JsonObject
        {
            ["name"] = name,
            ["icon"] = icon,
            ["url"] = url
        });
    }
}
