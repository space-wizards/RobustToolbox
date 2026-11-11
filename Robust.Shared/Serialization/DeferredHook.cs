namespace Robust.Shared.Serialization;

/// <summary>
///     A deferred <see cref="ISerializationHooks.AfterDeserialization"/> call, paired with a description of
///     whatever was being deserialized when the hook was queued.
/// </summary>
/// <remarks>
///     Deferred hooks are queued from a parallel deserialization pass and run later on the game thread, in an
///     order deliberately unrelated to the order they were queued in. A hook therefore has no way of telling
///     what it belongs to. Carrying the description alongside it lets a hook that logs a problem name the
///     prototype responsible instead of printing a bare message that has to be hunted down by hand.
/// </remarks>
public readonly struct DeferredHook
{
    public readonly ISerializationHooks Hooks;

    /// <summary>
    ///     Human-readable description of whatever queued this hook, such as a prototype's kind and id, or null
    ///     if it was queued without one.
    /// </summary>
    public readonly string? Context;

    public DeferredHook(ISerializationHooks hooks, string? context)
    {
        Hooks = hooks;
        Context = context;
    }
}
