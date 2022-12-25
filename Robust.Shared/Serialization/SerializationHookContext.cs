using System.Threading.Channels;

namespace Robust.Shared.Serialization;

public sealed class SerializationHookContext
{
    public static readonly SerializationHookContext DoSkipHooks = new(null, true);
    public static readonly SerializationHookContext DontSkipHooks = new(null, false);

#pragma warning disable CS0618
    public readonly ChannelWriter<ISerializationHooks>? DeferQueue;
    public readonly bool SkipHooks;

    public SerializationHookContext(ChannelWriter<ISerializationHooks>? deferQueue, bool skipHooks)
    {
        DeferQueue = deferQueue;
        SkipHooks = skipHooks;
    }

    public static SerializationHookContext ForSkipHooks(bool skip) => skip ? DoSkipHooks : DontSkipHooks;
#pragma warning restore CS0618
}
