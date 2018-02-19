using System;

namespace SS14.Shared.GameObjects
{
    public interface IEntityEventSubscriber { }

    public delegate void EntityEventHandler<in T>(object sender, T ev)
        where T : EntityEventArgs;

    public class EntityEventArgs : EventArgs { }
}
