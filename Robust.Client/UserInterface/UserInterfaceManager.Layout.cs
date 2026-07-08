using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;
internal sealed partial class UserInterfaceManager
{
    private readonly List<Control> _modalStack = new();

        private void RunMeasure(Control control)
        {
            if (control.IsMeasureValid || !control.IsInsideTree)
                return;

            if (control.Parent != null)
            {
                RunMeasure(control.Parent);
            }

            if (control is WindowRoot root)
            {
                control.Measure(root.Window.RenderTarget.Size / root.UIScale);
            }
            else if (control.PreviousMeasure.HasValue)
            {
                control.Measure(control.PreviousMeasure.Value);
            }
        }

        private void RunArrange(Control control)
        {
            if (control.IsArrangeValid || !control.IsInsideTree)
                return;

            if (control.Parent != null)
            {
                RunArrange(control.Parent);
            }

            if (control is WindowRoot root)
            {
                control.Arrange(UIBox2.FromDimensions(Vector2.Zero, root.Window.RenderTarget.Size / root.UIScale));
            }
            else if (control.PreviousArrange.HasValue)
            {
                control.Arrange(control.PreviousArrange.Value);
            }
        }

        public void Popup(string contents, string? title = null, bool clipboardButton = true)
        {
            var popup = new DefaultWindow
            {
                Title = string.IsNullOrEmpty(title) ? Loc.GetString("popup-title") : title,
            };

            var label = new RichTextLabel { Text = $"[color=white]{FormattedMessage.EscapeText(contents)}[/color]" };

            var vBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
            };

            vBox.AddChild(label);

            if (clipboardButton)
            {
                var copyButton = new Button
                {
                    Text = Loc.GetString("popup-copy-button"),
                    HorizontalExpand = true,
                };

                copyButton.OnPressed += _ =>
                {
                    _clipboard.SetText(contents);
                };

                var hBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalAlignment = Control.HAlignment.Right,
                };

                hBox.AddChild(copyButton);
                vBox.AddChild(hBox);
            }

            popup.Contents.AddChild(vBox);

            popup.OpenCentered();
        }

        public void ControlHidden(Control control)
        {
            // Does the same thing but it could later be changed so..
            ControlRemovedFromTree(control);
        }

        public void ControlRemovedFromTree(Control control)
        {
            if (control.Root?.StoredKeyboardFocus == control)
                control.Root.StoredKeyboardFocus = null;

            ReleaseKeyboardFocus(control);
            RemoveModal(control);

            if (control == ControlFocused)
                ControlFocused = null;

            if (control == CurrentlyHovered)
                UpdateHovered();
        }

        public void PushModal(Control modal)
        {
            _modalStack.Add(modal);
        }

        public void RemoveModal(Control modal)
        {
            if (_modalStack.Remove(modal))
            {
                modal.ModalRemoved();
            }
        }

        public void Render(IRenderHandle renderHandle)
        {
            // Render secondary windows LAST.
            // This makes it so that (hopefully) the GPU will be done rendering secondary windows
            // by the times we try to blit to them at the end of Clyde's render cycle,
            // So that the GL driver doesn't have to block on glWaitSync.

            foreach (var root in _roots)
            {
                if (root.Window != _clyde.MainWindow)
                {
                    using var _ = _prof.Group("Window");
                    _prof.WriteValue("ID", ProfData.Int32((int) root.Window.Id));

                    renderHandle.RenderInRenderTarget(
                        root.Window.RenderTarget,
                        () => DoRender(root),
                        root.ActualBgColor);
                }
            }

            using (_prof.Group("Main"))
            {
                DoRender(_windowsToRoot[_clyde.MainWindow.Id]);
            }

        void DoRender(WindowRoot root)
        {
            try
            {
                var total = 0;
                var drawingHandle = renderHandle.DrawingHandleScreen;
                drawingHandle.SetTransform(Matrix3x2.Identity);
                RenderControl(renderHandle, ref total, root, Vector2i.Zero, Color.White, null, Matrix3x2.Identity);
                drawingHandle.SetTransform(Matrix3x2.Identity);
                OnPostDrawUIRoot?.Invoke(new PostDrawUIRootEventArgs(root, drawingHandle));

                _prof.WriteValue("Controls rendered", ProfData.Int32(total));
            }
            catch (Exception e)
            {
                _sawmillUI.Error($"Caught exception while trying to draw a UI element: {root}");
                _runtime.LogException(e, nameof(UserInterfaceManager));
            }
        }
    }

        public void QueueStyleUpdate(Control control)
        {
            _styleUpdateQueue.Enqueue(control);
        }

        /// <summary>
        /// Queues a control so that it gets remeasured in the next frame update. Does not queue an arrange update.
        /// </summary>
        public void QueueMeasureUpdate(Control control)
        {
            _measureUpdateQueue.Enqueue(control);
        }

        /// <summary>
        /// Queues a control so that it gets rearranged in the next frame update. Does not queue a measure update.
        /// </summary>
        public void QueueArrangeUpdate(Control control)
        {
            _arrangeUpdateQueue.Enqueue(control);
        }
}
