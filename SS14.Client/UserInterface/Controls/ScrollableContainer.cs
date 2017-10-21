using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Components
{
    public class ScrollableContainer : Control
    {
        private const float BorderSize = 1.0f;

        public readonly List<Control> Components = new List<Control>();
        protected readonly IResourceCache ResourceCache;
        protected readonly Scrollbar ScrollbarH;
        protected readonly Scrollbar ScrollbarV;
        
        protected bool Disposing;

        private RenderImage _clippingRi;
        private Control _innerFocus;
        private float _maxX;
        private float _maxY;

        public ScrollableContainer(string uniqueName, Vector2i size, IResourceCache resourceCache)
        {
            ResourceCache = resourceCache;
            Size = size;

            BackgroundColor = new Color4(169, 169, 169, 255);
            DrawBackground = false;
            DrawBorder = true;

            _clippingRi = new RenderImage(uniqueName, (uint) Size.X, (uint) Size.Y);
            _clippingRi.BlendSettings.ColorSrcFactor = BlendMode.Factor.SrcAlpha;
            _clippingRi.BlendSettings.ColorDstFactor = BlendMode.Factor.OneMinusSrcAlpha;
            _clippingRi.BlendSettings.AlphaSrcFactor = BlendMode.Factor.SrcAlpha;
            _clippingRi.BlendSettings.AlphaDstFactor = BlendMode.Factor.OneMinusSrcAlpha;

            ScrollbarH = new Scrollbar(true, ResourceCache);
            ScrollbarV = new Scrollbar(false, ResourceCache);
            ScrollbarV.size = Size.Y;

            ScrollbarH.Update(0);
            ScrollbarV.Update(0);

            Update(0);
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            // ugh...
            ScrollbarH.size = ScrollbarV.Visible ? Size.X - ScrollbarV.ClientArea.Width : Size.X;
            ScrollbarV.size = ScrollbarH.Visible ? Size.Y - ScrollbarH.ClientArea.Height : Size.Y;

            _clientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i((int) _clippingRi.Width, (int) _clippingRi.Height));
        }

        protected override void OnCalcPosition()
        {
            ScrollbarH.Position = new Vector2i(ClientArea.Left, ClientArea.Bottom - ScrollbarH.ClientArea.Height);
            ScrollbarV.Position = new Vector2i(ClientArea.Right - ScrollbarV.ClientArea.Width, ClientArea.Top);

            base.OnCalcPosition();

            _maxX = 0;
            _maxY = 0;

            foreach (var component in Components)
            {
                if (component.Position.X + component.ClientArea.Width > _maxX)
                    _maxX = component.Position.X + component.ClientArea.Width;

                if (component.Position.Y + component.ClientArea.Height > _maxY)
                    _maxY = component.Position.Y + component.ClientArea.Height;
            }
        }

        public override void DoLayout()
        {

            
            foreach (var component in Components)
            {
                component.DoLayout();
                component.Position = new Vector2i(component.Position.X - (int)ScrollbarH.Value, component.Position.Y - (int)ScrollbarV.Value);
                component.Update(0);
            }

            base.DoLayout();
        }

        public override void Update(float frameTime)
        {
            if (Disposing || !Visible) return;

            if (_innerFocus != null && !Components.Contains(_innerFocus)) ClearFocus();

            ScrollbarH.max = (int) _maxX - ClientArea.Width + (_maxY > _clippingRi.Height ? ScrollbarV.ClientArea.Width : 0);
            ScrollbarH.Visible = (_maxX > _clippingRi.Width);

            ScrollbarV.max = (int) _maxY - ClientArea.Height + (_maxX > _clippingRi.Height ? ScrollbarH.ClientArea.Height : 0);
            ScrollbarV.Visible = (_maxY > _clippingRi.Height);

            ScrollbarH.Update(frameTime);
            ScrollbarV.Update(frameTime);
        }

        public override void Draw()
        {
            if (Disposing || !Visible) return;

            base.Draw();

            // the rectangle should always be completely covered with draws, no point clearing
            //_clippingRi.Clear((DrawBackground ? BackgroundColor : Color4.Transparent).Convert());

            _clippingRi.BeginDrawing();

            foreach (var component in Components)
            {
                if (_innerFocus != null && component == _innerFocus) continue;

                //var oldPos = component.Position;
                //component.Position = new Vector2i(component.Position.X - (int) ScrollbarH.Value, component.Position.Y - (int) ScrollbarV.Value);
                component.Update(0); //2 Updates per frame D:
                component.Draw();

                //component.Position = oldPos;
                //component.Update(0);
            }

            if (_innerFocus != null)
            {
                var oldPos = _innerFocus.Position;
                _innerFocus.Position = new Vector2i(_innerFocus.Position.X - (int) ScrollbarH.Value,
                    _innerFocus.Position.Y - (int) ScrollbarV.Value);

                _innerFocus.Update(0); //2 Updates per frame D:
                _innerFocus.Draw();
                _innerFocus.Position = oldPos;
                _innerFocus.Update(0);
            }

            _clippingRi.EndDrawing();
            _clippingRi.Blit(Position.X, Position.Y, _clippingRi.Height, _clippingRi.Width, Color.White, BlitterSizeMode.None);

            ScrollbarH.Draw();
            ScrollbarV.Draw();

            /*
            if (DrawBorder)
            {
                var screenRect = ClientArea.Translated(Position);
                CluwneLib.drawHollowRectangle(screenRect.Left, screenRect.Top, screenRect.Width, screenRect.Height, BorderSize, Color4.Black);
            }
            */
        }

        public override void Dispose()
        {
            if (Disposing) return;
            Disposing = true;
            Components.ForEach(c => c.Dispose());
            Components.Clear();
            _clippingRi.Dispose();
            _clippingRi = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (Disposing || !Visible) return false;

            if (ScrollbarH.MouseDown(e))
            {
                SetFocus(ScrollbarH);
                return true;
            }
            if (ScrollbarV.MouseDown(e))
            {
                SetFocus(ScrollbarV);
                return true;
            }

            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                var pos = new Vector2i(e.X - Position.X + (int)ScrollbarH.Value,
                    e.Y - Position.Y + (int)ScrollbarV.Value);

                MouseButtonEventArgs modArgs = new MouseButtonEventArgs(e.Button, pos);

                foreach (var component in Components)
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

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (Disposing || !Visible) return false;
            if (ScrollbarH.MouseUp(e)) return true;
            if (ScrollbarV.MouseUp(e)) return true;

            if (ClientArea.Contains(e.X, e.Y))
            {
                var pos = new Vector2i(e.X - (Position.X + (int)ScrollbarH.Value),
                    e.Y - (Position.Y + (int)ScrollbarV.Value));

                MouseButtonEventArgs modArgs = new MouseButtonEventArgs(e.Button, pos);

                foreach (var component in Components)
                {
                    component.MouseUp(modArgs);
                }
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (Disposing || !Visible) return;
            ScrollbarH.MouseMove(e);
            ScrollbarV.MouseMove(e);

            var pos = new Vector2i(e.X - (Position.X + (int)ScrollbarH.Value),
                e.Y - (Position.Y + (int)ScrollbarV.Value));
            MouseMoveEventArgs modArgs = new MouseMoveEventArgs(pos);

            foreach (var component in Components)
            {
                component.MouseMove(modArgs);
            }
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (_innerFocus != null)
            {
                if (_innerFocus.MouseWheelMove(e))
                    return true;
                if (ScrollbarV.Visible && ClientArea.Contains(e.X, e.Y))
                {
                    ScrollbarV.MouseWheelMove(e);
                    return true;
                }
            }
            else if (ScrollbarV.Visible && ClientArea.Contains(e.X, e.Y))
            {
                ScrollbarV.MouseWheelMove(e);
                return true;
            }
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            foreach (var component in Components)
            {
                if (component.KeyDown(e)) return true;
            }
            return false;
        }

        public override bool TextEntered(TextEventArgs e)
        {
            foreach (var component in Components)
            {
                if (component.TextEntered(e)) return true;
            }
            return false;
        }

        public void ResetScrollbars()
        {
            if (ScrollbarH.Visible) ScrollbarH.Value = 0;
            if (ScrollbarV.Visible) ScrollbarV.Value = 0;
        }

        private void SetFocus(Control newFocus)
        {
            if (_innerFocus != null)
            {
                _innerFocus.Focus = false;
                _innerFocus = newFocus;
                newFocus.Focus = true;
            }
            else
            {
                _innerFocus = newFocus;
                newFocus.Focus = true;
            }
        }

        private void ClearFocus()
        {
            if (_innerFocus != null)
            {
                _innerFocus.Focus = false;
                _innerFocus = null;
            }
        }
    }
}
