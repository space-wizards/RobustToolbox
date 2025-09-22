using System;
using Robust.Client.Graphics;
using Robust.Shared;

namespace Robust.Client.UserInterface.Controls
{
    public sealed class WindowRoot : UIRoot
    {
        private PopupContainer? _modalRoot;
        private LayoutContainer? _popupRoot;

        internal WindowRoot(IClydeWindow window)
        {
            Window = window;
        }
        public override float UIScale => UIScaleSet;
        internal float UIScaleSet { get; set; }

        /// <summary>
        /// Set after the window is resized, to batch up UI scale updates on window resizes.
        /// </summary>
        internal bool UIScaleUpdateNeeded { get; set; }

        public override IClydeWindow Window { get; }

        /// <summary>
        /// Disable automatic scaling of window <see cref="UIScale"/> based on resolution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Disabled by default for non-main windows as those most likely are smaller popup windows,
        /// that won't make sense with the default parameters.
        /// </para>
        /// </remarks>
        /// <seealso cref="CVars.ResAutoScaleEnabled"/>
        public bool DisableAutoScaling { get; set; } = true;

        public override PopupContainer ModalRoot => _modalRoot ?? throw new InvalidOperationException(
            $"Tried to access root controls without calling {nameof(CreateRootControls)}!");

        public override LayoutContainer PopupRoot => _popupRoot ?? throw new InvalidOperationException(
            $"Tried to access root controls without calling {nameof(CreateRootControls)}!");

        /// <summary>
        /// Creates root controls (e.g. <see cref="UIRoot.ModalRoot"/>) that are necessary for the UI system to
        /// fully function.
        /// </summary>
        /// <remarks>
        /// This should be called *after* inserting the main content into this instance,
        /// so that the created root controls (e.g. popups) correctly stay on top.
        /// </remarks>
        public void CreateRootControls()
        {
            if (_modalRoot != null)
                throw new InvalidOperationException("We've already created root controls!");

            _modalRoot = new PopupContainer
            {
                Name = nameof(ModalRoot),
                MouseFilter = MouseFilterMode.Ignore,
            };
            AddChild(_modalRoot);

            _popupRoot = new LayoutContainer
            {
                Name = nameof(PopupRoot),
                MouseFilter = MouseFilterMode.Ignore
            };
            AddChild(_popupRoot);
        }
    }
}
