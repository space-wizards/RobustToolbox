using Robust.Shared.Maths;
using System;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    [Control.ControlWrapAttribute("res://Engine/Scenes/SS14Window/SS14Window.tscn")]
    public class SS14Window : Panel
    {
        public const string StyleClassWindowTitle = "windowTitle";
        public const string StyleClassWindowPanel = "windowPanel";
        public const string StyleClassWindowHeader = "windowHeader";
        public const string StyleClassWindowCloseButton = "windowCloseButton";

        [Dependency] private readonly IDisplayManager _displayManager;

        protected virtual Vector2? CustomSize => null;

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

        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/SS14Window/SS14Window.tscn");

        public Control Contents { get; private set; }
        private TextureButton CloseButton;

        private const int DRAG_MARGIN_SIZE = 7;

        // TODO: Unhardcode this header size.
        private const float HEADER_SIZE_Y = 25;
        protected virtual Vector2 ContentsMinimumSize => new Vector2(50, 50);

        protected override Vector2 CalculateMinimumSize()
        {
            var marginSize = new Vector2(Contents.MarginLeft - Contents.MarginRight,
                Contents.MarginTop - Contents.MarginBottom);
            return ContentsMinimumSize + marginSize;
        }

        private DragMode CurrentDrag = DragMode.None;
        private Vector2 DragOffsetTopLeft;
        private Vector2 DragOffsetBottomRight;

        /// <summary>
        ///     If true, the window will simply be hidden if closed.
        ///     If false, the window control will be disposed entirely.
        /// </summary>
        public bool HideOnClose { get; set; } = false;

        public bool Resizable { get; set; } = true;

        private Label TitleLabel => GetChild("Header").GetChild<Label>("Header Text");

        public string Title
        {
            get => TitleLabel.Text;
            set => TitleLabel.Text = value;
        }

        /// <summary>
        ///     Invoked when the close button of this window is pressed.
        /// </summary>
        public event Action OnClose;

        // Drag resizing and moving code is mostly taken from Godot's WindowDialog.

        protected override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            var header = GetChild<Panel>("Header");
            CloseButton = header.GetChild<TextureButton>("CloseButton");
            CloseButton.OnPressed += CloseButtonPressed;

            Contents = GetChild("Contents");

            TitleLabel.AddStyleClass(StyleClassWindowTitle);
            AddStyleClass(StyleClassWindowPanel);
            header.AddStyleClass(StyleClassWindowHeader);
            // TODO: Remove this check by making UI in Godot also use CSS.
            if (!GameController.OnGodot)
            {
                header.PanelOverride = null;
                CloseButton.TextureNormal = null;
                CloseButton.AddStyleClass(StyleClassWindowCloseButton);
            }

            if (CustomSize != null)
            {
                Size = CustomSize.Value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                CloseButton.OnPressed -= CloseButtonPressed;
            }
        }

        private void CloseButtonPressed(BaseButton.ButtonEventArgs args)
        {
            Close();
        }

        public virtual void Close()
        {
            OnClose?.Invoke();
            if (HideOnClose)
            {
                Visible = false;
            }
            else
            {
                Dispose();
            }
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
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

        protected internal override void MouseUp(GUIMouseButtonEventArgs args)
        {
            base.MouseUp(args);

            DragOffsetTopLeft = DragOffsetBottomRight = Vector2.Zero;
            CurrentDrag = DragMode.None;

            // If this is done in MouseDown, Godot won't fire MouseUp as you need focus to receive MouseUps.
            UserInterfaceManager.KeyboardFocused?.ReleaseKeyboardFocus();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (CurrentDrag == DragMode.Move)
            {
                var globalPos = args.GlobalPosition;
                globalPos = Vector2.Clamp(globalPos, Vector2.Zero, _displayManager.ScreenSize);
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
                var minsize = CombinedMinimumSize;
                if ((CurrentDrag & DragMode.Top) == DragMode.Top)
                {
                    var MaxY = bottom - minsize.Y;
                    top = Math.Min(args.GlobalPosition.Y - DragOffsetTopLeft.Y, MaxY);
                }
                else if ((CurrentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + DragOffsetBottomRight.Y, top + minsize.Y);
                }

                if ((CurrentDrag & DragMode.Left) == DragMode.Left)
                {
                    var MaxX = right - minsize.X;
                    left = Math.Min(args.GlobalPosition.X - DragOffsetTopLeft.X, MaxX);
                }
                else if ((CurrentDrag & DragMode.Right) == DragMode.Right)
                {
                    right = Math.Max(args.GlobalPosition.X + DragOffsetBottomRight.X, left + minsize.X);
                }

                var rect = new UIBox2(left, top, right, bottom);
                Position = rect.TopLeft;
                Size = rect.Size;
            }
        }

        protected internal override void MouseExited()
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
                throw new InvalidOperationException(
                    "Window is not a child of the window root! You need to call AddToScreen first!");
            }

            SetPositionLast();
        }

        public bool IsAtFront()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException(
                    "Window is not a child of the window root! You need to call AddToScreen first!");
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

        public void AddToScreen()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
            }

            UserInterfaceManager.WindowRoot.AddChild(this);
        }

        public void Open()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException(
                    "Window is not a child of the window root! You need to call AddToScreen first!");
            }

            Visible = true;
            MoveToFront();
        }

        public void OpenCentered()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException(
                    "Window is not a child of the window root! You need to call AddToScreen first!");
            }

            Position = (Parent.Size - Size) / 2;
            Open();
        }

        public void OpenToLeft()
        {
            if (Parent != UserInterfaceManager.WindowRoot)
            {
                throw new InvalidOperationException(
                    "Window is not a child of the window root! You need to call AddToScreen first!");
            }

            Position = new Vector2(0, (Parent.Size.Y - Size.Y) / 2);
        }

        // Prevent window headers from getting off screen due to game window resizes.
        protected override void Update(ProcessFrameEventArgs args)
        {
            var windowSize = _displayManager.ScreenSize;
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

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            if (CustomSize.HasValue && (property == "margin_left" ||
                 property == "margin_right" ||
                 property == "margin_bottom" ||
                 property == "margin_top"))
            {
                return;
            }

            base.SetGodotProperty(property, value, context);
        }
    }
}
