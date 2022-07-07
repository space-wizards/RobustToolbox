using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;

namespace Robust.Client.UserInterface;
internal sealed partial class UserInterfaceManager
{
        private readonly List<WindowRoot> _roots = new();
        private readonly Dictionary<WindowId, WindowRoot> _windowsToRoot = new();
        public IEnumerable<UIRoot> AllRoots => _roots;

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

        public void Popup(string contents, string title = "Alert!")
        {
            var popup = new DefaultWindow
            {
                Title = title
            };

            popup.Contents.AddChild(new Label {Text = contents});
            popup.OpenCentered();
        }

        public void ControlHidden(Control control)
        {
            // Does the same thing but it could later be changed so..
            ControlRemovedFromTree(control);
        }

        public void ControlRemovedFromTree(Control control)
        {
            ReleaseKeyboardFocus(control);
            RemoveModal(control);
            if (control == CurrentlyHovered)
            {
                control.MouseExited();
                CurrentlyHovered = null;
                _clearTooltip();
            }

            if (control != ControlFocused) return;
            ControlFocused = null;
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
                var total = 0;
                _render(renderHandle, ref total, root, Vector2i.Zero, Color.White, null);
                var drawingHandle = renderHandle.DrawingHandleScreen;
                drawingHandle.SetTransform(Vector2.Zero, Angle.Zero, Vector2.One);
                OnPostDrawUIRoot?.Invoke(new PostDrawUIRootEventArgs(root, drawingHandle));

                _prof.WriteValue("Controls rendered", ProfData.Int32(total));
            }
        }

        public void QueueStyleUpdate(Control control)
        {
            _styleUpdateQueue.Enqueue(control);
        }

        public void QueueMeasureUpdate(Control control)
        {
            _measureUpdateQueue.Enqueue(control);
            _arrangeUpdateQueue.Enqueue(control);
        }

        public void QueueArrangeUpdate(Control control)
        {
            _arrangeUpdateQueue.Enqueue(control);
        }

        public WindowRoot CreateWindowRoot(IClydeWindow window)
        {
            if (_windowsToRoot.ContainsKey(window.Id))
            {
                throw new ArgumentException("Window already has a UI root.");
            }

            var newRoot = new WindowRoot(window)
            {
                MouseFilter = Control.MouseFilterMode.Ignore,
                HorizontalAlignment = Control.HAlignment.Stretch,
                VerticalAlignment = Control.VAlignment.Stretch,
                UIScaleSet = window.ContentScale.X
            };

            _roots.Add(newRoot);
            _windowsToRoot.Add(window.Id, newRoot);

            newRoot.StyleSheetUpdate();
            newRoot.InvalidateMeasure();
            QueueMeasureUpdate(newRoot);

            return newRoot;
        }

        public void DestroyWindowRoot(IClydeWindow window)
        {
            // Destroy window root if this window had one.
            if (!_windowsToRoot.TryGetValue(window.Id, out var root))
                return;

            _windowsToRoot.Remove(window.Id);
            _roots.Remove(root);

            root.RemoveAllChildren();
        }

        public WindowRoot? GetWindowRoot(IClydeWindow window)
        {
            return !_windowsToRoot.TryGetValue(window.Id, out var root) ? null : root;
        }
}
