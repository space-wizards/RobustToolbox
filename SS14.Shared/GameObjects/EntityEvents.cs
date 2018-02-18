using System;
using SS14.Shared.Input;

namespace SS14.Shared.GameObjects
{
    public interface IEntityEventSubscriber { }

    public delegate void EntityEventHandler<in T>(object sender, T ev)
        where T : EntityEventArgs;

    public class EntityEventArgs : EventArgs { }

    public class ClickedOnEntityEventArgs : EntityEventArgs
    {
        public EntityUid Clicker { get; set; }
        public EntityUid Clicked { get; set; }
        public ClickType MouseButton { get; set; }
    }
}
