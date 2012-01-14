using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;

namespace SS3D.UserInterface
{
    class ScrollableContainer : GuiComponent
    {
        protected Scrollbar scrollbarH;
        protected Scrollbar scrollbarV;

        protected RenderImage clippingRI;

        public List<GuiComponent> components = new List<GuiComponent>();

        protected float max_x = 0;
        protected float max_y = 0;

        public Color BackgroundColor = Color.DarkGray;
        public bool DrawBackground = false;

        protected Size size;

        protected IGuiComponent inner_focus;

        protected bool disposing = false;

        public ScrollableContainer(string uniqueName, Size _size)
            : base()
        {
            size = _size;

            if (RenderTargetCache.Targets.Contains(uniqueName)) //Now this is an ugly hack to work around duplicate RenderImages. Have to fix this later.
                uniqueName = uniqueName + System.Guid.NewGuid().ToString();

            clippingRI = new RenderImage(uniqueName, size.Width, size.Height, ImageBufferFormats.BufferRGB888A8);
            scrollbarH = new Scrollbar(); //If you arrived here because of a duplicate key error:
            scrollbarH.size = size.Width; //The name for scrollable containers and all classes that inherit them
            scrollbarH.Horizontal = true; //(Windows, dialog boxes etc) must be unique. Only one instance with a given name.
            scrollbarV = new Scrollbar();
            scrollbarV.size = size.Height;

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
            clientArea = new Rectangle(position, new Size(clippingRI.Width, clippingRI.Height));

            scrollbarH.Position = new Point(clientArea.X, clientArea.Bottom - scrollbarH.ClientArea.Height);
            scrollbarV.Position = new Point(clientArea.Right - scrollbarV.ClientArea.Width, clientArea.Y);

            if (scrollbarV.IsVisible()) scrollbarH.size = size.Width - scrollbarV.ClientArea.Width;
            else scrollbarH.size = size.Width;

            if (scrollbarH.IsVisible()) scrollbarV.size = size.Height - scrollbarH.ClientArea.Height;
            else scrollbarV.size = size.Height;

            max_x = 0;
            max_y = 0;

            foreach (GuiComponent component in components)
            {
                if (component.Position.X + component.ClientArea.Width > max_x) max_x = component.Position.X  + component.ClientArea.Width;
                if (component.Position.Y + component.ClientArea.Height > max_y) max_y = component.Position.Y  + component.ClientArea.Height;
            }

            scrollbarH.max = (int)max_x - clientArea.Width + scrollbarV.ClientArea.Width;
            if (max_x > clippingRI.Width) scrollbarH.SetVisible(true);
            else scrollbarH.SetVisible(false);

            scrollbarV.max = (int)max_y - clientArea.Height + scrollbarH.ClientArea.Height;
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
            if (DrawBackground) Gorgon.Screen.FilledRectangle(0, 0, clientArea.Width, clientArea.Height, BackgroundColor);
            foreach (GuiComponent component in components)
            {
                Point oldPos = component.Position;
                component.Position = new Point(component.Position.X - (int)scrollbarH.Value, component.Position.Y - (int)scrollbarV.Value);
                component.Update(); //2 Updates per frame D:
                component.Render();
                component.Position = oldPos;
                component.Update();
            }
            scrollbarH.Render();
            scrollbarV.Render();
            clippingRI.EndDrawing();
            clippingRI.Blit(position.X, position.Y);
            Gorgon.Screen.Rectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, System.Drawing.Color.Black);
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

            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                MouseInputEventArgs modArgs = new MouseInputEventArgs
                    (e.Buttons,
                    e.ShiftButtons,
                    new Vector2D(e.Position.X - position.X + scrollbarH.Value, e.Position.Y - position.Y + scrollbarV.Value),
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
                new Vector2D(e.Position.X - position.X + scrollbarH.Value, e.Position.Y - position.Y + scrollbarV.Value),
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
                new Vector2D(e.Position.X - position.X + scrollbarH.Value, e.Position.Y - position.Y + scrollbarV.Value),
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
