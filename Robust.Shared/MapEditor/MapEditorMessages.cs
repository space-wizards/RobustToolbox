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
}
