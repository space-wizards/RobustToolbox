using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.GameObjects.Events
{
    public class BoundKeyChangeEventArgs : Shared.GameObjects.Events.BoundKeyChangeEventArgs
    {
        public IEntity Actor { get; set; }
    }
}
