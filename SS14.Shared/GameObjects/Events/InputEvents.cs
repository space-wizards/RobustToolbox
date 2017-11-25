namespace SS14.Shared.GameObjects.Events
{
    public class BoundKeyChangeEventArgs : EntityEventArgs
    {
        private BoundKeyFunctions keyFunction;
        private BoundKeyState keyState;

        public BoundKeyFunctions KeyFunction { get => keyFunction; set => keyFunction = value; }
        public BoundKeyState KeyState { get => keyState; set => keyState = value; }
    }
}
