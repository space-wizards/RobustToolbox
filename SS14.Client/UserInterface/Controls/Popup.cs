using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Popup))]
    public class Popup : Control
    {
        public Popup() : base()
        {
        }

        public Popup(string name) : base()
        {
        }

        internal Popup(Godot.Popup control) : base(control)
        {
        }

        new private Godot.Popup SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Popup();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Popup) control;
        }

        public void Open(UIBox2? box = null)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Popup_(box?.Convert());
            }
        }

        public void OpenCentered()
        {
            if (GameController.OnGodot)
            {
                SceneControl.PopupCentered();
            }
        }

        public void OpenMinimum()
        {
            if (GameController.OnGodot)
            {
                SceneControl.PopupCenteredMinsize();
            }
        }
    }
}
