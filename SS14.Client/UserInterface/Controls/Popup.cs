using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("Popup")]
    public class Popup : Control
    {
        public Popup() : base()
        {
        }

        public Popup(string name) : base()
        {
        }

        #if GODOT
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
        #endif

        public void Open(UIBox2? box = null)
        {
            #if GODOT
            SceneControl.Popup_(box?.Convert());
            #endif
        }

        public void OpenCentered()
        {
            #if GODOT
            SceneControl.PopupCentered();
            #endif
        }

        public void OpenMinimum()
        {
            #if GODOT
            SceneControl.PopupCenteredMinsize();
            #endif
        }
    }
}
