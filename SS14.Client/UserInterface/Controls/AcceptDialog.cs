using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("AcceptDialog")]
    public class AcceptDialog : WindowDialog
    {
        public AcceptDialog() : base()
        {
        }
        public AcceptDialog(string name) : base(name)
        {
        }

        #if GODOT
        internal AcceptDialog(Godot.AcceptDialog control) : base(control)
        {
        }

        new private Godot.AcceptDialog SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.AcceptDialog();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.AcceptDialog)control;
        }
        #endif

        public string DialogText
        {
            #if GODOT
            get => SceneControl.DialogText;
            set => SceneControl.DialogText = value;
            #else
            get => default;
            set { }
            #endif
        }
    }
}
