using System;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    // ReSharper disable once InconsistentNaming
    public class SS14Window : BaseWindow
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

        private Label TitleLabel;

        public string Title
        {
            get => TitleLabel.Text;
            set => TitleLabel.Text = value;
        }

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

        public void OpenCenteredMinSize()
        {
            LayoutContainer.SetSize(this, ContentsMinimumSize);
            OpenCentered();
        }

        protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            var mode = DragMode.None;

            if (Resizable)
            {
                if (relativeMousePos.Y < SS14Window.DRAG_MARGIN_SIZE)
                {
                    mode = DragMode.Top;
                }
                else if (relativeMousePos.Y > Size.Y - SS14Window.DRAG_MARGIN_SIZE)
                {
                    mode = DragMode.Bottom;
                }

                if (relativeMousePos.X < SS14Window.DRAG_MARGIN_SIZE)
                {
                    mode |= DragMode.Left;
                }
                else if (relativeMousePos.X > Size.X - SS14Window.DRAG_MARGIN_SIZE)
                {
                    mode |= DragMode.Right;
                }
            }

            if (mode == DragMode.None && relativeMousePos.Y < SS14Window.HEADER_SIZE_Y)
            {
                mode = DragMode.Move;
            }

            return mode;
        }
    }
}
