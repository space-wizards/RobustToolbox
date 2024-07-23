using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

// Why did this dinky class grow to this LOC...

/// <summary>
/// Stores structured information about why a connection was denied or disconnected.
/// </summary>
/// <remarks>
/// <para>
/// The core networking layer (Lidgren) allows passing plain strings for disconnect reasons.
/// We can beam a structured format (like JSON) over this,
/// but Lidgren also generates messages internally (such as on timeout).
/// This class is responsible for bridging the two to produce consistent results.
/// </para>
/// <para>
/// Disconnect messages are just a simple key/value format.
/// Valid value types are <see cref="int"/>, <see cref="float"/>, <see cref="bool"/>, and <see cref="string"/>.
/// </para>
/// </remarks>
public sealed class NetDisconnectMessage
{
    private const string LidgrenDisconnectedPrefix = "Disconnected: ";

    /// <summary>
    /// The reason given if none was included in the structured message.
    /// </summary>
    internal const string DefaultReason = "unknown reason";

    /// <summary>
    /// The default redial flag given if none was included in the structured message.
    /// </summary>
    internal const bool DefaultRedialFlag = false;

    /// <summary>
    /// The key of the <see cref="Reason"/> value.
    /// </summary>
    public const string ReasonKey = "reason";

    /// <summary>
    /// The key of the <see cref="RedialFlag"/> value.
    /// </summary>
    public const string RedialKey = "redial";

    internal readonly Dictionary<string, object> Values;

    internal NetDisconnectMessage(Dictionary<string, object> values)
    {
        Values = values;
    }

    internal NetDisconnectMessage(
        string reason = DefaultReason,
        bool redialFlag = DefaultRedialFlag)
    {
        Values = new Dictionary<string, object>
        {
            { ReasonKey, reason },
            { RedialKey, redialFlag }
        };
    }

    /// <summary>
    /// The human-readable reason for why the disconnection happened.
    /// </summary>
    /// <seealso cref="ReasonKey"/>
    public string Reason => StringOf(ReasonKey, DefaultReason);

    /// <summary>
    /// Whether the client should "redial" to reconnect to the server.
    /// </summary>
    /// <remarks>
    /// Redial means the client gets restarted by the launcher, to enable an update to occur.
    /// This is generally set if the disconnection reason is some sort of version mismatch.
    /// </remarks>
    /// <seealso cref="RedialKey"/>
    public bool RedialFlag => BoolOf(RedialKey, DefaultRedialFlag);

    /// <summary>
    /// Decode from a disconnect message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If structured JSON can be extracted, it is used.
    /// Otherwise, or if the format is invalid, the entire input is returned as disconnect reason.
    /// </para>
    /// <para>Invalid JSON values (e.g. arrays) are discarded.</para>
    /// </remarks>
    /// <param name="text">The disconnect reason from Lidgren's disconnect message.</param>
    internal static NetDisconnectMessage Decode(string text)
    {
        var start = text.AsMemory().TrimStart();
        // Lidgren generates this prefix internally.
        if (start.Span.StartsWith(LidgrenDisconnectedPrefix))
            start = start[LidgrenDisconnectedPrefix.Length..];
        // If it starts with { it's probably a JSON object.
        if (start.Span.StartsWith("{"))
        {
            try
            {
                using var node = JsonDocument.Parse(start);
                DebugTools.Assert(node.RootElement.ValueKind == JsonValueKind.Object);
                return JsonToReason(node.RootElement);
            }
            catch (Exception)
            {
                // Discard the exception
            }
        }

        // Something went wrong. That probably means it's not a structured reason.
        // Or worst case scenario, some poor end-user has to look at half-broken JSON.
        return new NetDisconnectMessage(new Dictionary<string, object>
        {
            { ReasonKey, text }
        });
    }

    /// <summary>
    /// Encode to a textual string, that can be embedded into a disconnect message.
    /// </summary>
    internal string Encode()
    {
        return JsonSerializer.Serialize(Values);
    }

    private static NetDisconnectMessage JsonToReason(JsonElement obj)
    {
        DebugTools.Assert(obj.ValueKind == JsonValueKind.Object);

        var dict = new Dictionary<string, object>();
        foreach (var property in obj.EnumerateObject())
        {
            object value;
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    value = property.Value.GetString()!;
                    break;
                case JsonValueKind.Number:
                    if (property.Value.TryGetInt32(out var valueInt))
                        value = valueInt;
                    else
                        value = property.Value.GetSingle();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    value = property.Value.GetBoolean();
                    break;
                default:
                    // Discard invalid values intentionally.
                    continue;
            }

            dict[property.Name] = value;
        }

        return new NetDisconnectMessage(dict);
    }

    /// <summary>
    /// Get a value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <returns>
    /// Null if no such value exists, otherwise an object of one of the valid types (int, float, string, bool).
    /// </returns>
    public object? ValueOf(string key)
    {
        return Values.GetValueOrDefault(key);
    }

    /// <summary>
    /// Get a <see cref="string"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <param name="defaultValue">Default value to return if the value does not exist or is the wrong type.</param>
    /// <returns>
    /// The <see cref="string"/> value with the given key,
    /// or <paramref name="defaultValue"/> if no such value exists or it's a different type.
    /// </returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? StringOf(string key, string? defaultValue = null)
    {
        if (ValueOf(key) is not string valueString)
            return defaultValue;

        return valueString;
    }

    /// <summary>
    /// Get a <see cref="bool"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <returns>
    /// The <see cref="bool"/> value with the given key, or <see langword="null" /> if no such value exists or it's a different type.
    /// </returns>
    public bool? BoolOf(string key) => ValueOf(key) as bool?;

    /// <summary>
    /// Get a <see cref="bool"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <param name="defaultValue">Default value to return if the value does not exist or is the wrong type.</param>
    /// <returns>
    /// The <see cref="bool"/> value with the given key,
    /// or <paramref name="defaultValue"/> if no such value exists or it's a different type.
    /// </returns>
    public bool BoolOf(string key, bool defaultValue) => BoolOf(key) ?? defaultValue;

    /// <summary>
    /// Get a <see cref="int"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <returns>
    /// The <see cref="int"/> value with the given key, or <see langword="null" /> if no such value exists or it's a different type.
    /// </returns>
    public int? Int32Of(string key) => ValueOf(key) as int?;

    /// <summary>
    /// Get an <see cref="Int32"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <param name="defaultValue">Default value to return if the value does not exist or is the wrong type.</param>
    /// <returns>
    /// The <see cref="Int32"/> value with the given key,
    /// or <paramref name="defaultValue"/> if no such value exists or it's a different type.
    /// </returns>
    public int Int32Of(string key, int defaultValue) => Int32Of(key) ?? defaultValue;

    /// <summary>
    /// Get a <see cref="float"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <returns>
    /// The <see cref="float"/> value with the given key, or <see langword="null" /> if no such value exists or it's a different type.
    /// </returns>
    public float? SingleOf(string key)
    {
        var value = ValueOf(key);
        return value as float? ?? value as int?;
    }

    /// <summary>
    /// Get a <see cref="Single"/> value by its key.
    /// </summary>
    /// <param name="key">The key of the value to look up.</param>
    /// <param name="defaultValue">Default value to return if the value does not exist or is the wrong type.</param>
    /// <returns>
    /// The <see cref="Single"/> value with the given key,
    /// or <paramref name="defaultValue"/> if no such value exists or it's a different type.
    /// </returns>
    public float SingleOf(string key, float defaultValue) => SingleOf(key) ?? defaultValue;
}
