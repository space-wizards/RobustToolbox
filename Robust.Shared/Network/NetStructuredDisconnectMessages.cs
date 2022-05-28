using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Robust.Shared.Network;

/// <summary>
/// Structured disconnection utilities.
/// These use JsonNode so that Content may add it's own data.
/// Note that to prevent encoding a NetStructuredDisco value within a NetStructuredDisco value,
/// these should be encoded at the "highest level".
/// Whatever generates the final "reason" value is responsible for performing NetStructuredDisco.Encode.
/// </summary>
public static class NetStructuredDisconnectMessages
{
    public const string ReasonKey = "reason";
    public const string RedialKey = "redial";

    /// <summary>
    /// Encodes a structured disconnect message into a JsonObject.
    /// That can then be extended with additional properties.
    /// </summary>
    public static JsonObject EncodeObject(string text, bool redialFlag = false)
    {
        JsonObject obj = new();
        obj[ReasonKey] = text;
        obj[RedialKey] = redialFlag;
        return obj;
    }

    /// <summary>
    /// Encodes a structured disconnect message.
    /// Note that using this kind of gets in the way of adding content properties.
    /// </summary>
    public static string Encode(string text, bool redialFlag = false) => Encode(EncodeObject(text, redialFlag));

    /// <summary>
    /// Encodes a structured disconnect message from a JsonObject.
    /// </summary>
    public static string Encode(JsonObject obj) => obj.ToJsonString();

    /// <summary>
    /// Decodes a structured disconnect message.
    /// This is designed assuming the input isn't necessarily a structured disconnect message.
    /// As such this will always produce output that can be passed to ReasonOf.
    /// </summary>
    public static JsonObject Decode(string text)
    {
        var start = text.AsSpan().TrimStart();
        // Lidgren generates this prefix internally.
        var lidgrenDisconnectedPrefix = "Disconnected: ";
        if (start.StartsWith(lidgrenDisconnectedPrefix))
            start = start.Slice(lidgrenDisconnectedPrefix.Length);
        // If it starts with { it's probably a JSON object.
        if (start.StartsWith("{"))
        {
            try
            {
                var node = JsonNode.Parse(new string(start));
                if (node != null)
                    return (JsonObject)node;
            }
            catch (Exception)
            {
                // Discard the exception
            }
        }

        // Something went wrong, so...
        JsonObject fallback = new();
        fallback[ReasonKey] = text;
        return fallback;
    }

    /// <summary>
    /// Get a property as a JsonValue.
    /// </summary>
    public static JsonValue? ValueOf(JsonObject obj, string key)
    {
        if (obj.TryGetPropertyValue(key, out var val))
            if (val is JsonValue)
                return ((JsonValue) val);
        return null;
    }

    /// <summary>
    /// Decode a string property.
    /// </summary>
    public static string StringOf(JsonObject obj, string key, string def)
    {
        var val = ValueOf(obj, key);
        if (val != null && val.TryGetValue(out string? res))
            return res;
        return def;
    }

    /// <summary>
    /// Grab the redial flag.
    /// </summary>
    public static bool BoolOf(JsonObject obj, string key, bool def)
    {
        var val = ValueOf(obj, key);
        if (val != null && val.TryGetValue(out bool res))
            return res;
        return def;
    }

    /// <summary>
    /// Decode a reason.
    /// </summary>
    public static string ReasonOf(JsonObject obj) => StringOf(obj, ReasonKey, "unknown reason");

    /// <summary>
    /// Grab the redial flag.
    /// </summary>
    public static bool RedialFlagOf(JsonObject obj) => BoolOf(obj, RedialKey, false);
}

