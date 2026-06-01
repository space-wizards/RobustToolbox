using System.Threading;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Serialization;

/// <summary>
/// Wraps a component state so the same state can be written to multiple players' streams in a single PVS
/// pass without re-serializing it. Serializing a state object is self-contained (varints are value-encoded,
/// the mapped-string table is frozen, no stream back-references), so its bytes are position-independent and
/// can be spliced verbatim into any player's stream.
/// </summary>
internal sealed class ReusableComponentState(IComponentState inner, uint tick, uint fromTick, byte[]? bytes = null) : IComponentState
{
    /// <summary>The real component state. Written inline when <see cref="Bytes"/> is still null.</summary>
    public readonly IComponentState Inner = inner;

    private byte[]? _bytes = bytes;

    /// <summary>The tick this state was computed for.</summary>
    public readonly uint Tick = tick;

    /// <summary>The from-tick this state was computed for.</summary>
    public readonly uint FromTick = fromTick;


    /// <summary>The pre-serialized bytes if materialized, else null. Volatile so the write pass sees fills.</summary>
    public byte[]? Bytes => Volatile.Read(ref _bytes);

    /// <summary>Publishes materialized bytes. Idempotent under races (equal arrays); last writer wins.</summary>
    public void SetBytes(byte[] bytes) => Volatile.Write(ref _bytes, bytes);
}
