using System;
using System.Threading.Channels;

namespace Robust.Shared.Serialization;

public sealed class SerializationHookContext
{
    public static readonly SerializationHookContext DoSkipHooks = new(null, true);
    public static readonly SerializationHookContext DontSkipHooks = new(null, false);

#pragma warning disable CS0618
    public readonly ChannelWriter<DeferredHook>? DeferQueue;
    public readonly bool SkipHooks;

    /// <summary>
    ///     Description of whatever is currently being deserialized, such as a prototype's kind and id. Attached
    ///     to every hook queued through <see cref="DeferQueue"/> and surfaced as <see cref="RunningContext"/>
    ///     while that hook runs, so a deferred hook can report what it belongs to.
    /// </summary>
    public readonly string? Context;

    public SerializationHookContext(ChannelWriter<DeferredHook>? deferQueue, bool skipHooks)
        : this(deferQueue, skipHooks, null)
    {
    }

    public SerializationHookContext(ChannelWriter<DeferredHook>? deferQueue, bool skipHooks, string? context)
    {
        DeferQueue = deferQueue;
        SkipHooks = skipHooks;
        Context = context;
    }

    public static SerializationHookContext ForSkipHooks(bool skip) => skip ? DoSkipHooks : DontSkipHooks;
#pragma warning restore CS0618

    [ThreadStatic]
    private static string? _runningContext;

    /// <summary>
    ///     Description of whatever queued the deferred hook currently running on this thread, or null when no
    ///     deferred hook is running. Purely for diagnostics: it lets a hook that logs a warning or error name
    ///     the prototype at fault.
    /// </summary>
    public static string? RunningContext => _runningContext;

    /// <summary>
    ///     Sets <see cref="RunningContext"/> for as long as the returned scope is alive. Used to wrap a single
    ///     deferred hook call.
    /// </summary>
    internal static RunningContextScope EnterRunningContext(string? context) => new(context);

    internal readonly struct RunningContextScope : IDisposable
    {
        private readonly string? _previous;

        internal RunningContextScope(string? context)
        {
            _previous = _runningContext;
            _runningContext = context;
        }

        public void Dispose() => _runningContext = _previous;
    }
}
