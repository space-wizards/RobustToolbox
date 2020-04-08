using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    // ReSharper disable once InconsistentNaming
    public class SS14Window : Control
    {
        public const string StyleClassWindowTitle = "windowTitle";
        public const string StyleClassWindowPanel = "windowPanel";
        public const string StyleClassWindowHeader = "windowHeader";
        public const string StyleClassWindowCloseButton = "windowCloseButton";

        protected virtual Vector2? CustomSize => null;

        public SS14Window()
        {
            MouseFilter = MouseFilterMode.Stop;

            AddChild(new PanelContainer
            {
                StyleClasses = {StyleClassWindowPanel}
            });

            AddChild(new VBoxContainer
            {
                SeparationOverride = 0,
                Children =
                {
                    new PanelContainer
                    {
                        StyleClasses = {StyleClassWindowHeader},
                        CustomMinimumSize = (0, HEADER_SIZE_Y),
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new MarginContainer
                                    {
                                        MarginLeftOverride = 5,
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            (TitleLabel = new Label
                                            {
                                                StyleIdentifier = "foo",
                                                ClipText = true,
                                                Text = "Exemplary Window Title Here",
                                                VAlign = Label.VAlignMode.Center,
                                                StyleClasses = {StyleClassWindowTitle}
                                            })
                                        }
                                    },
                                    (CloseButton = new TextureButton
                                    {
                                        StyleClasses = {StyleClassWindowCloseButton},
                                        SizeFlagsVertical = SizeFlags.ShrinkCenter
                                    })
                                }
                            }
                        }
                    },
                    (Contents = new MarginContainer
                    {
                        MarginBottomOverride = 10,
                        MarginLeftOverride = 10,
                        MarginRightOverride = 10,
                        MarginTopOverride = 10,
                        RectClipContent = true,
                        SizeFlagsVertical = SizeFlags.FillExpand
                    })
                }
            });

            CloseButton.OnPressed += CloseButtonPressed;

            if (CustomSize != null)
            {
                LayoutContainer.SetSize(this, CustomSize.Value);
            }
        }

        public MarginContainer Contents { get; private set; }
        private TextureButton CloseButton;

        private const int DRAG_MARGIN_SIZE = 7;

        // TODO: Un-hard code this header size.
        private const float HEADER_SIZE_Y = 25;
        protected virtual Vector2 ContentsMinimumSize => (50, 50);

        protected override Vector2 CalculateMinimumSize()
        {
            return Vector2.ComponentMax(ContentsMinimumSize, base.CalculateMinimumSize());
        }

        private DragMode CurrentDrag = DragMode.None;
        private Vector2 DragOffsetTopLeft;
        private Vector2 DragOffsetBottomRight;

        public bool Resizable { get; set; } = true;

        private Label TitleLabel;

        public string Title
        {
            get => TitleLabel.Text;
            set => TitleLabel.Text = value;
        }

        public bool IsOpen => Parent != null;

        /// <summary>
        ///     Invoked when the close button of this window is pressed.
        /// </summary>
        public event Action OnClose;

        // Drag resizing and moving code is mostly taken from Godot's WindowDialog.

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

            if (!args.CanFocus)
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

            if (!args.CanFocus)
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
                var (minSizeX, minSizeY) = CombinedMinimumSize;
                if ((CurrentDrag & DragMode.Top) == DragMode.Top)
                {
                    var maxY = bottom - minSizeY;
                    top = Math.Min(args.GlobalPosition.Y - DragOffsetTopLeft.Y, maxY);
                }
                else if ((CurrentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + DragOffsetBottomRight.Y, top + minSizeY);
                }

                if ((CurrentDrag & DragMode.Left) == DragMode.Left)
                {
                    var maxX = right - minSizeX;
                    left = Math.Min(args.GlobalPosition.X - DragOffsetTopLeft.X, maxX);
                }
                else if ((CurrentDrag & DragMode.Right) == DragMode.Right)
                {
                    right = Math.Max(args.GlobalPosition.X + DragOffsetBottomRight.X, left + minSizeX);
                }

                var rect = new UIBox2(left, top, right, bottom);
                LayoutContainer.SetPosition(this, rect.TopLeft);
                LayoutContainer.SetSize(this, rect.Size);
            }
        }

        protected internal override void MouseExited()
        {
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
            Open();
            LayoutContainer.SetPosition(this, (Parent.Size - Size) / 2);
        }

        public void OpenCenteredMinSize()
        {
            LayoutContainer.SetSize(this, ContentsMinimumSize);
            OpenCentered();
        }

        public void OpenToLeft()
        {
            Open();
            LayoutContainer.SetPosition(this, (0, (Parent.Size.Y - Size.Y) / 2));
        }

        protected virtual void Opened()
        {

        }

        // Prevent window headers from getting off screen due to game window resizes.

        protected override void Update(FrameEventArgs args)
        {
            var (spaceX, spaceY) = Parent.Size;
            if (Position.Y > spaceY)
            {
                LayoutContainer.SetPosition(this, (Position.X, spaceY - HEADER_SIZE_Y));
            }

            if (Position.X > spaceX)
            {
                // 50 is arbitrary here. As long as it's bumped back into view.
                LayoutContainer.SetPosition(this, (spaceX - 50, Position.Y));
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

        [Flags]
        private enum DragMode
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
