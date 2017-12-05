using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.GameObjects.Events
{
    public class BoundKeyChangeEventArgs : SS14.Shared.GameObjects.Events.BoundKeyChangeEventArgs
    {
        public IEntity Actor { get; set; }
    }
}
