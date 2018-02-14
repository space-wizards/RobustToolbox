using System;
using SS14.Shared.Console;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.GameObjects
{
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

    public class SpriteChangedMsg : ComponentMessage { }

    public class BumpedEntMsg : ComponentMessage
    {
        public IEntity Entity { get; }

        public BumpedEntMsg(IEntity entity)
        {
            Entity = entity;
        }
    }

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

    public class DescriptionStringMsg : ComponentMessage
    {
        public string DescriptionString { get; set; } = String.Empty;
    }
}
