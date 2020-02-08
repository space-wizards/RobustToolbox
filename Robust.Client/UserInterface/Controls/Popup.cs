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

        public event Action OnPopupHide;

        private Vector2 _desiredSize;

        public void Open(UIBox2? box = null)
        {
            if (Visible)
            {
                UserInterfaceManagerInternal.RemoveModal(this);
            }

            if (box != null)
            {
                PopupContainer.SetPopupOrigin(this, box.Value.TopLeft);

                _desiredSize = box.Value.Size;
            }

            Visible = true;
            UserInterfaceManagerInternal.PushModal(this);
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
