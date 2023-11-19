using System.IO;
using Microsoft.IO;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.Memory;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Generic memory manager for engine use.
/// </summary>
internal sealed class RobustMemoryManager
{
    // Let's be real this is a bandaid for pooling bullshit at an engine level and I don't know what
    // good memory management looks like for PVS or the RobustSerializer.

    private static readonly RecyclableMemoryStreamManager MemStreamManager = new()
    {
        ThrowExceptionOnToArray = true,
    };

    public RobustMemoryManager()
    {
        MemStreamManager.StreamDoubleDisposed += (sender, args) =>
            throw new InvalidMemoryOperationException("Found double disposed stream.");

        MemStreamManager.StreamFinalized += (sender, args) =>
            throw new InvalidMemoryOperationException("Stream finalized but not disposed indicating a leak");

        MemStreamManager.StreamOverCapacity += (sender, args) =>
            throw new InvalidMemoryOperationException("Stream over memory capacity");
    }

    public static MemoryStream GetMemoryStream()
    {
        var stream = MemStreamManager.GetStream();
        DebugTools.Assert(stream.Position == 0);
        return stream;
    }
}
