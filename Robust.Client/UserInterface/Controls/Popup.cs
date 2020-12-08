using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class Popup : Control
    {
        public Popup()
        {
            Visible = false;
        }

        public event Action? OnPopupHide;

        private Vector2 _desiredSize;

        public void Open(UIBox2? box = null, Vector2? altPos = null)
        {
            if (Visible)
            {
                UserInterfaceManagerInternal.RemoveModal(this);
            }

            if (box != null &&
                (_desiredSize != box.Value.Size ||
                 PopupContainer.GetPopupOrigin(this) != box.Value.TopLeft ||
                 PopupContainer.GetAltOrigin(this) != altPos))
            {
                PopupContainer.SetPopupOrigin(this, box.Value.TopLeft);
                PopupContainer.SetAltOrigin(this, altPos);

                _desiredSize = box.Value.Size;
                MinimumSizeChanged();
            }

            Visible = true;
            UserInterfaceManagerInternal.PushModal(this);
        }

        public void Close()
        {
            if (!Visible) return;
            UserInterfaceManagerInternal.RemoveModal(this);
        }


        protected internal override void ModalRemoved()
        {
            base.ModalRemoved();

            Visible = false;
            OnPopupHide?.Invoke();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return Vector2.ComponentMax(_desiredSize, base.CalculateMinimumSize());
        }
    }
}
