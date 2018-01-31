using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Client.Utility;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Reflection;
using System;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    public class SS14Window : Control
    {
        public SS14Window() : base()
        {
        }
        public SS14Window(string name) : base(name)
        {
        }

        [Flags]
        enum DragMode
        {
            None = 0,
            Move = 1,
            Top = 1 << 1,
            Bottom = 1 << 2,
            Left = 1 << 3,
            Right = 1 << 4,
        }

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/SS14Window/SS14Window.tscn");
            return (Godot.Control)res.Instance();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = control;
        }

        new private Godot.Control SceneControl;

        public Control Contents { get; private set; }
        private TextureButton CloseButton;

        private const int DRAG_MARGIN_SIZE = 7;
        // TODO: Unhardcode this header size.
        private const float HEADER_SIZE_Y = 25;
        private static readonly Vector2 MinSize = new Vector2(50, 50);

        private DragMode CurrentDrag = DragMode.None;
        private Vector2 DragOffsetTopLeft;
        private Vector2 DragOffsetBottomRight;

        /// <summary>
        ///     If true, the window will simply be hidden if closed.
        ///     If false, the window control will be disposed entirely.
        /// </summary>
        public bool HideOnClose { get; set; } = false;

        public bool Resizable { get; set; } = true;

        // Drag resizing and moving code is mostly taken from Godot's WindowDialog.

        protected override void Initialize()
        {
            base.Initialize();

            var header = GetChild("Header");
            CloseButton = header.GetChild<TextureButton>("CloseButton");
            CloseButton.OnPressed += CloseButtonPressed;

            Contents = GetChild("Contents");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                CloseButton.OnPressed -= CloseButtonPressed;
                CloseButton = null;
                SceneControl = null;
                Contents = null;
                CloseButton = null;
            }
        }

        private void CloseButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (HideOnClose)
            {
                Visible = false;
            }
            else
            {
                Dispose();
            }
        }

        protected override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            CurrentDrag = GetDragModeFor(args.RelativePosition);

            if (CurrentDrag != DragMode.None)
            {
                DragOffsetTopLeft = args.GlobalPosition - Position;
                DragOffsetBottomRight = Position + Size - args.GlobalPosition;
            }

            MoveToFront();
        }

        protected override void MouseUp(GUIMouseButtonEventArgs args)
        {
            base.MouseUp(args);

            DragOffsetTopLeft = DragOffsetBottomRight = Vector2.Zero;
            CurrentDrag = DragMode.None;

            // If this is done in MouseDown, Godot won't fire MouseUp as you need focus to receive MouseUps.
            UserInterfaceManager.Focused?.ReleaseFocus();
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (CurrentDrag == DragMode.Move)
            {
                var globalPos = args.GlobalPosition;
                globalPos = Vector2.Clamp(globalPos, Vector2.Zero, Godot.OS.GetWindowSize().Convert());
                Position = globalPos - DragOffsetTopLeft;
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
                        cursor = CursorShape.VSplit;
                        break;

                    case DragMode.Left:
                    case DragMode.Right:
                        cursor = CursorShape.HSplit;
                        break;

                    case DragMode.Bottom | DragMode.Left:
                    case DragMode.Top | DragMode.Right:
                        cursor = CursorShape.BDiagSize;
                        break;

                    case DragMode.Bottom | DragMode.Right:
                    case DragMode.Top | DragMode.Left:
                        cursor = CursorShape.FDiagSize;
                        break;
                }

                DefaultCursorShape = cursor;
            }
            else
            {
                var top = Rect.Top;
                var bottom = Rect.Bottom;
                var left = Rect.Left;
                var right = Rect.Right;

                if ((CurrentDrag & DragMode.Top) == DragMode.Top)
                {
                    var MaxY = bottom - MinSize.Y;
                    top = Math.Min(args.GlobalPosition.Y - DragOffsetTopLeft.Y, MaxY);
                }
                else if ((CurrentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + DragOffsetBottomRight.Y, top + MinSize.Y);
                }

                if ((CurrentDrag & DragMode.Left) == DragMode.Left)
                {
                    var MaxX = right - MinSize.X;
                    left = Math.Min(args.GlobalPosition.X - DragOffsetTopLeft.X, MaxX);
                }
                else if ((CurrentDrag & DragMode.Right) == DragMode.Right)
                {
                    right = Math.Max(args.GlobalPosition.X + DragOffsetBottomRight.X, left + MinSize.X);
                }

                Position = new Vector2(left, top);
                var rect = new Box2(left, top, right, bottom);
                Size = new Vector2(rect.Width, rect.Height);
            }
        }

        protected override void MouseExited()
        {
            if (Resizable && CurrentDrag == DragMode.None)
            {
                DefaultCursorShape = CursorShape.Arrow;
            }
        }

        private DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            var mode = DragMode.None;

            if (Resizable)
            {
                if (relativeMousePos.Y < DRAG_MARGIN_SIZE)
                {
                    mode = DragMode.Top;
                }
                else if (relativeMousePos.Y > Size.Y - DRAG_MARGIN_SIZE)
                {
                    mode = DragMode.Bottom;
                }

                if (relativeMousePos.X < DRAG_MARGIN_SIZE)
                {
                    mode |= DragMode.Left;
                }
                else if (relativeMousePos.X > Size.X - DRAG_MARGIN_SIZE)
                {
                    mode |= DragMode.Right;
                }
            }

            if (mode == DragMode.None && relativeMousePos.Y < HEADER_SIZE_Y)
            {
                mode = DragMode.Move;
            }

            return mode;
        }

        public void MoveToFront()
        {
            var root = UserInterfaceManager.WindowRoot;
            if (Parent != root)
            {
                throw new InvalidOperationException("Window is not a child of the window root! You need to call AddToScreen first!");
            }
            root.SceneControl.MoveChild(SceneControl, root.SceneControl.GetChildCount());
        }

        public bool IsAtFront()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException("Window is not a child of the window root! You need to call AddToScreen first!");
            }
            var siblings = Parent.SceneControl.GetChildren();
            var ourPos = SceneControl.GetPositionInParent();
            for (var i = ourPos + 1; i < siblings.Length; i++)
            {
                if (siblings[i] is Godot.Control control)
                {
                    if (control.Visible)
                    {
                        // If we find a control after us that's visible, we're NOT in front.
                        return false;
                    }
                }
            }

            return true;
        }

        public void AddToScreen()
        {
            UserInterfaceManager.WindowRoot.AddChild(this);
        }

        public void Open()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException("Window is not a child of the window root! You need to call AddToScreen first!");
            }
            Visible = true;
            MoveToFront();
        }

        public void OpenCentered()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException("Window is not a child of the window root! You need to call AddToScreen first!");
            }
            Position = (Parent.Size - Size) / 2;
            Open();
        }

        // Prevent window headers from getting off screen due to game window resizes.
        protected override void Update(FrameEventArgs args)
        {
            var windowSize = Godot.OS.GetWindowSize().Convert();
            if (Position.Y > windowSize.Y)
            {
                Position = new Vector2(Position.X, windowSize.Y - HEADER_SIZE_Y);
            }
            if (Position.X > windowSize.X)
            {
                // 50 is arbitrary here. As long as it's bumped back into view.
                Position = new Vector2(windowSize.X - 50, Position.Y);
            }
        }
    }
}
