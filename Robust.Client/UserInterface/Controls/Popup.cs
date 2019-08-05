using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    public class Popup : Control
    {
        public Popup()
        {
        }

        public Popup(string name) : base(name)
        {
        }

        public event Action OnPopupHide;

        public void Open(UIBox2? box = null)
        {
            if (Visible)
            {
                UserInterfaceManagerInternal.RemoveModal(this);
            }

            if (box != null)
            {
                Position = box.Value.TopLeft;
                Size = box.Value.Size;
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

        protected override void SetDefaults()
        {
            base.SetDefaults();

            Visible = false;
        }
    }
}
