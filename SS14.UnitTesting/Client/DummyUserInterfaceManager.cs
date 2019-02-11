using System.Collections.Generic;
using SS14.Client;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Client
{
    internal class DummyUserInterfaceManager : IUserInterfaceManagerInternal
    {
        public UITheme Theme { get; } = new UIThemeDummy();

        public Control Focused => throw new System.NotImplementedException();

        public Control StateRoot => throw new System.NotImplementedException();

        public Control WindowRoot => throw new System.NotImplementedException();

        public Control CurrentlyHovered => throw new System.NotImplementedException();

        public Control RootControl => throw new System.NotImplementedException();

        public IDebugMonitors DebugMonitors => throw new System.NotImplementedException();

        public void DisposeAllComponents()
        {
            throw new System.NotImplementedException();
        }

        public void GDFocusEntered(Control control)
        {
            throw new System.NotImplementedException();
        }

        public void GDFocusExited(Control control)
        {
            throw new System.NotImplementedException();
        }

        public void GDMouseEntered(Control control)
        {
            throw new System.NotImplementedException();
        }

        public void GDMouseExited(Control control)
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

        public Control MouseGetControl(Vector2 coordinates)
        {
            throw new System.NotImplementedException();
        }

        public void GDPreKeyDown(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void GDPreKeyUp(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void Render(IRenderHandle renderHandle)
        {
            throw new System.NotImplementedException();
        }

        public Dictionary<(GodotAsset asset, int resourceId), object> GodotResourceInstanceCache { get; } =
            new Dictionary<(GodotAsset asset, int resourceId), object>();

        public void UnhandledKeyDown(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void UnhandledKeyUp(KeyEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void MouseUp(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void GDUnhandledMouseDown(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void GDUnhandledMouseUp(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void Update(ProcessFrameEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void FrameUpdate(RenderFrameEventArgs args)
        {
            throw new System.NotImplementedException();
        }

        public void MouseDown(MouseButtonEventArgs args)
        {
            throw new System.NotImplementedException();
        }
    }
}
