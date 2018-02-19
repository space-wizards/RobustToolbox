using System;
using SS14.Shared.Console;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class SaidSomethingMsg : ComponentMessage
    {
        public ChatChannel Channel { get; }
        public string Text { get; }

        public SaidSomethingMsg(ChatChannel channel, string text)
        {
            Channel = channel;
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public class SpriteChangedMsg : ComponentMessage { }

    [Serializable, NetSerializable]
    public class BumpedEntMsg : ComponentMessage
    {
        public IEntity Entity { get; }

        public BumpedEntMsg(IEntity entity)
        {
            Entity = entity;
        }
    }

    [Serializable, NetSerializable]
    public class BoundKeyChangedMsg : ComponentMessage
    {
        public BoundKeyFunctions Function { get; }
        public BoundKeyState State { get; }

        public BoundKeyChangedMsg(BoundKeyFunctions function, BoundKeyState state)
        {
            Directed = true;
            Function = function;
            State = state;
        }
    }

    [Serializable, NetSerializable]
    public class DescriptionStringMsg : ComponentMessage
    {
        public string DescriptionString { get; set; } = String.Empty;
    }

    [Serializable, NetSerializable]
    public class ClientChangedHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ClientChangedHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public class ClientEntityClickMsg : ComponentMessage
    {
        public EntityUid Uid { get; }
        public ClickType Click { get; }

        public ClientEntityClickMsg(EntityUid uid, ClickType click)
        {
            Directed = true;
            Uid = uid;
            Click = click;
        }
    }
}
