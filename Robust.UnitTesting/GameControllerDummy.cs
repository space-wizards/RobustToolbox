using Robust.Client;
using Robust.Client.Input;
using Robust.Client.Interfaces;
using Robust.Shared.Timing;

namespace Robust.UnitTesting
{
    internal sealed class GameControllerDummy : IGameControllerInternal
    {
        public void Shutdown(string reason = null)
        {
        }

        public bool LoadConfigAndUserData { get; set; } = true;

        public void Startup()
        {

        }

        public void MainLoop(GameController.DisplayMode mode)
        {

        }

        public string ContentRootDir { get; set; }

        public void KeyDown(KeyEventArgs keyEvent)
        {
        }

        public void KeyUp(KeyEventArgs keyEvent)
        {
        }

        public void TextEntered(TextEventArgs textEvent)
        {
        }

        public void MouseDown(MouseButtonEventArgs mouseEvent)
        {
        }

        public void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
        }

        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
        }

        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            throw new System.NotImplementedException();
        }
    }
}
