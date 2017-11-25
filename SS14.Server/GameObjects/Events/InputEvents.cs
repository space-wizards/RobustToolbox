using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.GameObjects.Events
{
    public class BoundKeyChangeEventArgs : SS14.Shared.GameObjects.Events.BoundKeyChangeEventArgs
    {
        private IEntity actor;

        public IEntity Actor { get => actor; set => actor = value; }
    }
}
