using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class ScrollableContainer : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        protected Scrollbar scrollbarH;
        protected Scrollbar scrollbarV;

        protected RenderImage clippingRI;

        public List<GuiComponent> components = new List<GuiComponent>();

        protected float max_x = 0;
        protected float max_y = 0;

        public Color BackgroundColor = Color.DarkGray;
        public bool DrawBackground = false;

        protected Size Size;

        protected IGuiComponent inner_focus;

        protected bool disposing = false;

        public ScrollableContainer(string uniqueName, Size size, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Size = size;

            if (RenderTargetCache.Targets.Contains(uniqueName)) //Now this is an ugly hack to work around duplicate RenderImages. Have to fix this later.
                uniqueName = uniqueName + System.Guid.NewGuid();

            clippingRI = new RenderImage(uniqueName, Size.Width, Size.Height, ImageBufferFormats.BufferRGB888A8);
            scrollbarH = new Scrollbar(true, _resourceManager); //If you arrived here because of a duplicate key error:
            scrollbarH.size = Size.Width; //The name for scrollable containers and all classes that inherit them
                                             //(Windows, dialog boxes etc) must be unique. Only one instance with a given name.
            scrollbarV = new Scrollbar(false, _resourceManager);
            scrollbarV.size = Size.Height;

            scrollbarH.Update();
            scrollbarV.Update();

            clippingRI.SourceBlend = AlphaBlendOperation.SourceAlpha;
            clippingRI.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;
            Update();
        }

        public void ResetScrollbars()
        {
            if (scrollbarH.IsVisible()) scrollbarH.Value = 0;
            if (scrollbarV.IsVisible()) scrollbarV.Value = 0;
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            ClientArea = new Rectangle(Position, new Size(clippingRI.Width, clippingRI.Height));

            scrollbarH.Position = new Point(ClientArea.X, ClientArea.Bottom - scrollbarH.ClientArea.Height);
            scrollbarV.Position = new Point(ClientArea.Right - scrollbarV.ClientArea.Width, ClientArea.Y);

            if (scrollbarV.IsVisible()) scrollbarH.size = Size.Width - scrollbarV.ClientArea.Width;
            else scrollbarH.size = Size.Width;

            if (scrollbarH.IsVisible()) scrollbarV.size = Size.Height - scrollbarH.ClientArea.Height;
            else scrollbarV.size = Size.Height;

            max_x = 0;
            max_y = 0;

            foreach (GuiComponent component in components)
            {
                if (component.Position.X + component.ClientArea.Width > max_x) max_x = component.Position.X  + component.ClientArea.Width;
                if (component.Position.Y + component.ClientArea.Height > max_y) max_y = component.Position.Y  + component.ClientArea.Height;
            }

            scrollbarH.max = (int)max_x - ClientArea.Width + (max_y > clippingRI.Height ? scrollbarV.ClientArea.Width : 0);
            if (max_x > clippingRI.Width) scrollbarH.SetVisible(true);
            else scrollbarH.SetVisible(false);

            scrollbarV.max = (int)max_y - ClientArea.Height + (max_x > clippingRI.Width ? scrollbarH.ClientArea.Height : 0);
            if (max_y > clippingRI.Height) scrollbarV.SetVisible(true);
            else scrollbarV.SetVisible(false);

            scrollbarH.Update();
            scrollbarV.Update();
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            clippingRI.Clear(System.Drawing.Color.Transparent);
            clippingRI.BeginDrawing();
            if (DrawBackground) Gorgon.Screen.FilledRectangle(0, 0, ClientArea.Width, ClientArea.Height, BackgroundColor);
            foreach (GuiComponent component in components)
            {
                Point oldPos = component.Position;
                component.Position = new Point(component.Position.X - (int)scrollbarH.Value, component.Position.Y - (int)scrollbarV.Value);
                component.Update(); //2 Updates per frame D:
                component.Render();
                component.Position = oldPos;
                component.Update();
            }
            clippingRI.EndDrawing();
            clippingRI.Blit(Position.X, Position.Y);
            scrollbarH.Render();
            scrollbarV.Render();
            Gorgon.Screen.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, System.Drawing.Color.Black);
        }

        public override void Dispose()
        {
            if (disposing) return;
            disposing = true;
            clippingRI.Dispose();
            clippingRI = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected void SetFocus(IGuiComponent newFocus)
        {
            if (inner_focus != null)
            {
                inner_focus.Focus = false;
                inner_focus = newFocus;
                newFocus.Focus = true;
            }
            else
            {
                inner_focus = newFocus;
                newFocus.Focus = true;
            }
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;

            if (scrollbarH.MouseDown(e))
            {
                SetFocus(scrollbarH);
                return true;
            }
            if (scrollbarV.MouseDown(e))
            {
                SetFocus(scrollbarV);
                return true;
            }

            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                MouseInputEventArgs modArgs = new MouseInputEventArgs
                    (e.Buttons,
                    e.ShiftButtons,
                    new Vector2D(e.Position.X - Position.X + scrollbarH.Value, e.Position.Y - Position.Y + scrollbarV.Value),
                    e.WheelPosition,
                    e.RelativePosition,
                    e.WheelDelta,
                    e.ClickCount);

                foreach (GuiComponent component in components)
                {
                    if (component.MouseDown(modArgs))
                    {
                        SetFocus(component);
                        return true;
                    }
                }
                return true;
            }

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (scrollbarH.MouseUp(e)) return true;
            if (scrollbarV.MouseUp(e)) return true;

            MouseInputEventArgs modArgs = new MouseInputEventArgs
                (e.Buttons,
                e.ShiftButtons,
                new Vector2D(e.Position.X - Position.X + scrollbarH.Value, e.Position.Y - Position.Y + scrollbarV.Value),
                e.WheelPosition,
                e.RelativePosition,
                e.WheelDelta,
                e.ClickCount);

            foreach (GuiComponent component in components)
                component.MouseUp(modArgs);

            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            scrollbarH.MouseMove(e);
            scrollbarV.MouseMove(e);

            MouseInputEventArgs modArgs = new MouseInputEventArgs
                (e.Buttons,
                e.ShiftButtons,
                new Vector2D(e.Position.X - Position.X + scrollbarH.Value, e.Position.Y - Position.Y + scrollbarV.Value),
                e.WheelPosition,
                e.RelativePosition,
                e.WheelDelta,
                e.ClickCount);

            foreach (GuiComponent component in components)
                component.MouseMove(modArgs);

            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (inner_focus != null)
                if(inner_focus.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            foreach (GuiComponent component in components)
                if (component.KeyDown(e)) return true;
            return false;
        }
    }
}
