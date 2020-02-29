using System;

namespace Robust.Shared.GameObjects
{
    public interface IEntityEventSubscriber { }

    public delegate void EntityEventHandler<in T>(T ev)
        where T : EntityEventArgs;

    public class EntityEventArgs : EventArgs { }
}
