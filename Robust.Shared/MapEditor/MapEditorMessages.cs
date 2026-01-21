using System;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.MapEditor;

internal static class MapEditorMessages
{
    [Serializable, NetSerializable]
    internal record struct ActionId(Guid Identifier)
    {
        public static ActionId Create() => new ActionId(Guid.NewGuid());
    }

    [Serializable, NetSerializable]
    internal sealed class StartEditing : EntityEventArgs;

    [Serializable, NetSerializable]
    internal sealed class CreateNewMap : EntityEventArgs;

    [Serializable, NetSerializable]
    internal sealed class CreateNewView : EntityEventArgs
    {
        public required NetEntity MapData;
        public required Vector2 Position;
        public required ActionId Action;
    }

    [Serializable, NetSerializable]
    internal sealed class DestroyView : EntityEventArgs
    {
        public required NetEntity Eye;
    }

    /// <summary>
    /// C->S, indicates client wants to save the file.
    /// </summary>
    /// <remarks>
    /// Server is expected to respond with <see cref="SaveMapData"/>
    /// </remarks>
    [Serializable, NetSerializable]
    internal sealed class SaveMap : EntityEventArgs
    {
        public required NetEntity MapData;
        public required MapFileHandle Handle;
        public string? NewName;
    }

    // Server -> Client
    [Serializable, NetSerializable]
    internal sealed class SaveMapData : EntityEventArgs
    {
        public required NetEntity MapData;
        public required MapFileHandle Handle;
        // Must be zstd-compressed.
        public required byte[] Data;
    }

    // Client -> Server
    [Serializable, NetSerializable]
    internal sealed class OpenMap : EntityEventArgs
    {
        // Must be zstd-compressed.
        public required byte[] Data;
        public MapFileHandle? Handle;
        public string? Name;
    }

    // Server -> Client
    [Serializable, NetSerializable]
    internal sealed class OpenMapFailed : EntityEventArgs
    {
        public required string Name;
        public MapFileHandle? Handle;
    }
}
