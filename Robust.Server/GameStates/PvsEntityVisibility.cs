namespace Robust.Server.GameStates;

public enum PvsEntityVisibility : byte
{
    Invalid = 0,

    /// <summary>
    /// The entity has never been sent to this client.
    /// </summary>
    Unsent,

    /// <summary>
    /// Entity entered PVS range. This can either mean:
    /// - The entity was not sent in the last state.
    /// - The entity was not in the last acked state
    /// - We told the client that the entity left their PVS range sometime after the last acked state.
    /// </summary>
    Entered,

    /// <summary>
    /// Entity stayed in PVS range, and hasn't been dirtied since the last acked state.
    /// </summary>
    StayedUnchanged,

    /// <summary>
    /// Entity stayed in PVS range, but was dirtied sometime after the last acked state.
    /// </summary>
    StayedChanged
}
