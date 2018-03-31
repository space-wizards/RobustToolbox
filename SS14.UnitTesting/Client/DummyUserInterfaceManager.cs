using SS14.Client;
using SS14.Client.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;

namespace SS14.UnitTesting.Client
{
    public class DummyUserInterfaceManager : IUserInterfaceManager
    {
        public Control Focused => throw new System.NotImplementedException();

        public Control StateRoot => throw new System.NotImplementedException();

        public Control WindowRoot => throw new System.NotImplementedException();

        public Control RootControl => throw new System.NotImplementedException();

        public bool ShowFPS { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool ShowCoordDebug { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public void DisposeAllComponents()
        {
            throw new System.NotImplementedException();
        }

        public void FocusEntered(Control control)
        {
            throw new System.NotImplementedException();
        }

        public void FocusExited(Control control)
        {
            throw new System.NotImplementedException();
        }

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }

        public void Popup(string contents, string title = "Alert!")
        {
            throw new System.NotImplementedException();
        }

        public void PreKeyDown(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void PreKeyUp(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void UnhandledKeyDown(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void UnhandledKeyUp(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void UnhandledMouseDown(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void UnhandledMouseUp(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void Update(ProcessFrameEventArgs args)
        {
            throw new System.NotImplementedException();
        }
    }
}
