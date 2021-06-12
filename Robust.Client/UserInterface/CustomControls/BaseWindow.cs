using System;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     Provides basic functionality for windows that can be opened, dragged around, etc...
    /// </summary>
    public abstract class BaseWindow : Control
    {
        private DragMode CurrentDrag = DragMode.None;
        private Vector2 DragOffsetTopLeft;
        private Vector2 DragOffsetBottomRight;

        protected bool _firstTimeOpened = true;

        public bool Resizable { get; set; } = true;
        public bool IsOpen => Parent != null;

        /// <summary>
        ///     Invoked when the close button of this window is pressed.
        /// </summary>
        public event Action? OnClose;

        public virtual void Close()
        {
            if (Parent == null)
            {
                return;
            }

            Parent.RemoveChild(this);
            OnClose?.Invoke();
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            CurrentDrag = GetDragModeFor(args.RelativePosition);

            if (CurrentDrag != DragMode.None)
            {
                DragOffsetTopLeft = args.PointerLocation.Position / UIScale - Position;
                DragOffsetBottomRight = Position + Size - args.PointerLocation.Position / UIScale;
            }

            MoveToFront();
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            DragOffsetTopLeft = DragOffsetBottomRight = Vector2.Zero;
            CurrentDrag = DragMode.None;

            // If this is done in MouseDown, Godot won't fire MouseUp as you need focus to receive MouseUps.
            UserInterfaceManager.KeyboardFocused?.ReleaseKeyboardFocus();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (Parent == null)
            {
                return;
            }

            if (CurrentDrag == DragMode.Move)
            {
                var globalPos = args.GlobalPosition;
                globalPos = Vector2.Clamp(globalPos, Vector2.Zero, Parent.Size);
                LayoutContainer.SetPosition(this, globalPos - DragOffsetTopLeft);
                return;
            }

            if (!Resizable)
            {
                return;
            }

            if (CurrentDrag == DragMode.None)
            {
                var cursor = CursorShape.Arrow;
                var previewDragMode = GetDragModeFor(args.RelativePosition);
                switch (previewDragMode)
                {
                    case DragMode.Top:
                    case DragMode.Bottom:
                        cursor = CursorShape.VResize;
                        break;

                    case DragMode.Left:
                    case DragMode.Right:
                        cursor = CursorShape.HResize;
                        break;

                    case DragMode.Bottom | DragMode.Left:
                    case DragMode.Top | DragMode.Right:
                        cursor = CursorShape.Crosshair;
                        break;

                    case DragMode.Bottom | DragMode.Right:
                    case DragMode.Top | DragMode.Left:
                        cursor = CursorShape.Crosshair;
                        break;
                }

                DefaultCursorShape = cursor;
            }
            else
            {
                var (left, top) = Position;
                var (right, bottom) = Position + SetSize;

                if ((CurrentDrag & DragMode.Top) == DragMode.Top)
                {
                    top = Math.Min(args.GlobalPosition.Y - DragOffsetTopLeft.Y, bottom);
                }
                else if ((CurrentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + DragOffsetBottomRight.Y, top);
                }

                if ((CurrentDrag & DragMode.Left) == DragMode.Left)
                {
                    left = Math.Min(args.GlobalPosition.X - DragOffsetTopLeft.X, right);
                }
                else if ((CurrentDrag & DragMode.Right) == DragMode.Right)
                {
                    right = Math.Max(args.GlobalPosition.X + DragOffsetBottomRight.X, left);
                }

                var rect = new UIBox2(left, top, right, bottom);
                LayoutContainer.SetPosition(this, rect.TopLeft);
                SetSize = rect.Size;

                /*
                var timing = IoCManager.Resolve<IGameTiming>();

                var l = GetValue<float>(LayoutContainer.MarginLeftProperty);
                var t = GetValue<float>(LayoutContainer.MarginTopProperty);

                Logger.Debug($"{timing.CurFrame}: {rect.TopLeft}/({l}, {t}), {rect.Size}/{SetSize}");
                */
            }
        }

        protected internal override void MouseExited()
        {
            base.MouseExited();

            if (Resizable && CurrentDrag == DragMode.None)
            {
                DefaultCursorShape = CursorShape.Arrow;
            }
        }

        public void MoveToFront()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("This window is not currently open.");
            }

            SetPositionLast();
        }

        public bool IsAtFront()
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("This window is not currently open");
            }

            var siblingCount = Parent.ChildCount;
            var ourPos = GetPositionInParent();
            for (var i = ourPos + 1; i < siblingCount; i++)
            {
                if (Parent.GetChild(i).Visible)
                {
                    // If we find a control after us that's visible, we're NOT in front.
                    return false;
                }
            }

            return true;
        }

        public void Open()
        {
            if (!Visible)
            {
                Visible = true;
                Logger.WarningS("ui", $"Window {this} had visibility false. Do not use visibility on SS14Window.");
            }

            if (!IsOpen)
            {
                UserInterfaceManager.WindowRoot.AddChild(this);
            }

            Opened();
        }

        public void OpenCentered()
        {
            if (_firstTimeOpened)
            {
                Measure(Vector2.Infinity);
                SetSize = DesiredSize;
                Open();
                // An explaination: The BadOpenGLVersionWindow was showing up off the top-left corner of the screen.
                // Basically, if OpenCentered happens super-early, RootControl doesn't get time to layout children.
                // But we know that this is always going to be one of the roots anyway for now.
                LayoutContainer.SetPosition(this, (UserInterfaceManager.RootControl.Size - SetSize) / 2);
                _firstTimeOpened = false;
            }
            else
            {
                Open();
            }
        }

        public void OpenToLeft()
        {
            if (_firstTimeOpened)
            {
                Measure(Vector2.Infinity);
                SetSize = DesiredSize;
                Open();
                LayoutContainer.SetPosition(this, (0, (Parent!.Size.Y - DesiredSize.Y) / 2));
                _firstTimeOpened = false;
            }
            else
            {
                Open();
            }
        }

        protected virtual void Opened()
        {

        }

        protected virtual DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            return DragMode.None;
        }

        [Flags]
        protected enum DragMode : byte
        {
            None = 0,
            Move = 1,
            Top = 1 << 1,
            Bottom = 1 << 2,
            Left = 1 << 3,
            Right = 1 << 4,
        }
    }
}
