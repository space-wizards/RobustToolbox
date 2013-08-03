using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class ScrollableContainer : GuiComponent //This is a note: Spooge wants support for mouseover-scrolling of scrollable containers inside other scrollable containers.
    {
        protected readonly IResourceManager _resourceManager;

        protected Scrollbar scrollbarH;
        protected Scrollbar scrollbarV;

        protected RenderImage clippingRI;

        public List<GuiComponent> components = new List<GuiComponent>();

        protected float max_x = 0;
        protected float max_y = 0;

        public Color BackgroundColor = Color.DarkGray;
        public bool DrawBackground = false;
        public bool DrawBorder = true;

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
            scrollbarH = new Scrollbar(true, _resourceManager);
            scrollbarV = new Scrollbar(false, _resourceManager);
            scrollbarV.size = Size.Height;

            scrollbarH.Update(0);
            scrollbarV.Update(0);

            clippingRI.SourceBlend = AlphaBlendOperation.SourceAlpha;
            clippingRI.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha;

            clippingRI.SourceBlendAlpha = AlphaBlendOperation.SourceAlpha;
            clippingRI.DestinationBlendAlpha = AlphaBlendOperation.InverseSourceAlpha;

            Update(0);
        }

        public void ResetScrollbars()
        {
            if (scrollbarH.IsVisible()) scrollbarH.Value = 0;
            if (scrollbarV.IsVisible()) scrollbarV.Value = 0;
        }

        public override void Update(float frameTime)
        {
            if (disposing || !IsVisible()) return;
            ClientArea = new Rectangle(Position, new Size(clippingRI.Width, clippingRI.Height));

            if (inner_focus != null && !components.Contains((GuiComponent)inner_focus)) ClearFocus();

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

            scrollbarH.Update(frameTime);
            scrollbarV.Update(frameTime);
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;

            clippingRI.Clear(DrawBackground ? BackgroundColor : System.Drawing.Color.Transparent);
            clippingRI.BeginDrawing();

            foreach (GuiComponent component in components)
            {
                if (inner_focus != null && component == inner_focus) continue;
                Point oldPos = component.Position;
                component.Position = new Point(component.Position.X - (int)scrollbarH.Value, component.Position.Y - (int)scrollbarV.Value);
                component.Update(0); //2 Updates per frame D:
                component.Render();
                component.Position = oldPos;
                component.Update(0);
            }

            if (inner_focus != null)
            {
                Point oldPos = inner_focus.Position;
                inner_focus.Position = new Point(inner_focus.Position.X - (int)scrollbarH.Value, inner_focus.Position.Y - (int)scrollbarV.Value);
                inner_focus.Update(0); //2 Updates per frame D:
                inner_focus.Render();
                inner_focus.Position = oldPos;
                inner_focus.Update(0);
            }

            clippingRI.EndDrawing();
            clippingRI.Blit(Position.X, Position.Y);

            scrollbarH.Render();
            scrollbarV.Render();

            if(DrawBorder) Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, System.Drawing.Color.Black);
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

        protected void ClearFocus()
        {
            if (inner_focus != null)
            {
                inner_focus.Focus = false;
                inner_focus = null;
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
            MouseInputEventArgs modArgs = new MouseInputEventArgs
                (e.Buttons,
                e.ShiftButtons,
                new Vector2D(e.Position.X - Position.X + scrollbarH.Value, e.Position.Y - Position.Y + scrollbarV.Value),
                e.WheelPosition,
                e.RelativePosition,
                e.WheelDelta,
                e.ClickCount);

            if (inner_focus != null)
            {
                if (inner_focus.MouseWheelMove(modArgs))
                    return true;
                else
                    if (scrollbarV.IsVisible() && ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                    {
                        scrollbarV.MouseWheelMove(e);
                        return true;
                    }
            }
            else if (scrollbarV.IsVisible() && ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                {
                    scrollbarV.MouseWheelMove(e);
                    return true;
                }
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
