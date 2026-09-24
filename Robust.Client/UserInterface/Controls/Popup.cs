using System;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class Popup : Control
    {
        public Popup()
        {
            Visible = false;
        }

        /// <summary>
        ///     Action that gets invoked just before the pop-up gets shown. This does not get invoked if the pop-up is
        ///     moved / re-opened in another location.
        /// </summary>
        public event Action? OnPopupOpen;

        /// <summary>
        ///     Action that gets invoked just after a pop-up becomes invisible. This does not get invoked if the pop-up
        ///     is moved / re-opened in another location.
        /// </summary>
        public event Action? OnPopupHide;

        private Vector2 _desiredSize;

        public bool CloseOnClick { get; set; } = true;

        public bool CloseOnEscape { get; set; } = true;

        private bool _autoOrphan;

        /// <summary>
        /// Opens the popup at the location of the mouse.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The popup is placed in the modal root, and is automatically sized.
        /// </para>
        /// <para>
        /// The popup *automatically* gets removed from the popup root when it is hidden again.
        /// Do not remove it manually!
        /// </para>
        /// </remarks>
        public void OpenAtMouse()
        {
            _autoOrphan = true;
            var root = UserInterfaceManager.GetRootForMouse();
            root.ModalRoot.AddChild(this);

            Open(UIBox2.FromDimensions(UserInterfaceManager.MousePositionScaled.Position, Vector2.One));
        }

        public virtual void Open(UIBox2? box = null, Vector2? altPos = null, Vector2? altPosUp = null)
        {
            if (Visible)
            {
                UserInterfaceManagerInternal.RemoveModal(this);
            }
            else
            {
                OnPopupOpen?.Invoke();
            }

            if (box != null &&
                (_desiredSize != box.Value.Size ||
                 PopupContainer.GetPopupOrigin(this) != box.Value.TopLeft ||
                 PopupContainer.GetAltOrigin(this) != altPos ||
                 PopupContainer.GetAltOriginUp(this) != altPosUp))
            {
                PopupContainer.SetPopupOrigin(this, box.Value.TopLeft);
                PopupContainer.SetAltOrigin(this, altPos);
                PopupContainer.SetAltOriginUp(this, altPosUp);

                _desiredSize = box.Value.Size;
                InvalidateMeasure();
            }

            Visible = true;
            UserInterfaceManagerInternal.PushModal(this);
        }

        public virtual void Close()
        {
            if (!Visible) return;
            UserInterfaceManagerInternal.RemoveModal(this);
        }


        protected internal override void ModalRemoved()
        {
            base.ModalRemoved();

            Visible = false;
            OnPopupHide?.Invoke();

            if (_autoOrphan)
            {
                Orphan();
                _autoOrphan = false;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return Vector2.Max(
                _desiredSize,
                base.MeasureOverride(Vector2.Max(availableSize, _desiredSize)));
        }
    }
}
