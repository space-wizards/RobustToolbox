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

        public Popup(Godot.Popup control) : base(control)
        {
        }

        new private Godot.Popup SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Popup();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Popup) control;
        }

        public void Open(UIBox2? box = null)
        {
            SceneControl.Popup_(box?.Convert());
        }

        public void OpenCentered()
        {
            SceneControl.PopupCentered();
        }

        public void OpenMinimum()
        {
            SceneControl.PopupCenteredMinsize();
        }
    }
}
