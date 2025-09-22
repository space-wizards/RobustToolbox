using System;
using System.IO;
using Microsoft.IO;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Generic memory manager for engine use.
/// </summary>
internal sealed class RobustMemoryManager
{
    // Let's be real this is a bandaid for pooling bullshit at an engine level and I don't know what
    // good memory management looks like for PVS or the RobustSerializer.

    private static readonly RecyclableMemoryStreamManager MemStreamManager = new(new RecyclableMemoryStreamManager.Options
    {
        ThrowExceptionOnToArray = true,
    });

    public RobustMemoryManager()
    {
        MemStreamManager.StreamDoubleDisposed += (sender, args) =>
            throw new InvalidOperationException("Found double disposed stream.");

        MemStreamManager.StreamFinalized += (sender, args) =>
            throw new InvalidOperationException("Stream finalized but not disposed indicating a leak");

        MemStreamManager.StreamOverCapacity += (sender, args) =>
            throw new InvalidOperationException("Stream over memory capacity");
    }

    public static MemoryStream GetMemoryStream()
    {
        var stream = MemStreamManager.GetStream("RobustMemoryManager");
        DebugTools.Assert(stream.Position == 0);
        return stream;
    }

    public static MemoryStream GetMemoryStream(int length)
    {
        var stream = MemStreamManager.GetStream("RobustMemoryManager", length);
        DebugTools.Assert(stream.Position == 0);
        return stream;
    }
}
