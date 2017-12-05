namespace SS14.Shared.GameObjects.Events
{
    public class BoundKeyChangeEventArgs : EntityEventArgs
    {
        public BoundKeyFunctions KeyFunction { get; set; }
        public BoundKeyState KeyState { get; set; }
    }
}
