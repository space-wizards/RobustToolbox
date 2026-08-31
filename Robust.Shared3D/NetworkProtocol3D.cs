using System.Text.Json;

namespace Robust.Shared3D;

public static class NetworkProtocol3D
{
    public const int Version = 1;
    public const int DefaultPort = 12133;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize<T>(T message)
    {
        return JsonSerializer.Serialize(message, JsonOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static string? ReadKind(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("kind", out var kind)
            ? kind.GetString()
            : null;
    }
}

public sealed record HelloMessage3D
{
    public string Kind { get; init; } = "hello";
    public int ProtocolVersion { get; init; } = NetworkProtocol3D.Version;
    public int PlayerId { get; init; }
    public float FixedDelta { get; init; }
}

public sealed record InputMessage3D
{
    public string Kind { get; init; } = "input";
    public ulong Sequence { get; init; }
    public float MovementX { get; init; }
    public float MovementY { get; init; }
    public bool Jump { get; init; }
    public float FacingYaw { get; init; }
}

public sealed record SnapshotMessage3D
{
    public string Kind { get; init; } = "snapshot";
    public long ServerTick { get; init; }
    public required PlayerSnapshot3D[] Players { get; init; }
}

public sealed record PlayerSnapshot3D
{
    public int PlayerId { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }
    public float VelocityZ { get; init; }
    public float FacingYaw { get; init; }
    public bool Grounded { get; init; }
    public ulong AcknowledgedInput { get; init; }
}
