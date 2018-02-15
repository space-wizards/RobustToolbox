using System;
using SS14.Shared.Console;
using SS14.Shared.Enums;
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
            Function = function;
            State = state;
        }
    }

    [Serializable, NetSerializable]
    public class BoundKeyRepeatMsg : ComponentMessage
    {
        public BoundKeyFunctions Function { get; }
        public BoundKeyState State { get; }

        public BoundKeyRepeatMsg(BoundKeyFunctions function, BoundKeyState state)
        {
            Function = function;
            State = state;
        }
    }

    [Serializable, NetSerializable]
    public class DescriptionStringMsg : ComponentMessage
    {
        public string DescriptionString { get; set; } = String.Empty;
    }
}
