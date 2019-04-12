using System;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
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
    public class BumpedEntMsg : ComponentMessage
    {
        public IEntity Entity { get; }

        public BumpedEntMsg(IEntity entity)
        {
            Entity = entity;
        }
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
