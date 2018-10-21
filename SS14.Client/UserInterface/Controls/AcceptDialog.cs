using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.AcceptDialog))]
    public class AcceptDialog : WindowDialog
    {
        public AcceptDialog() : base()
        {
        }

        public AcceptDialog(string name) : base(name)
        {
        }

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
            SceneControl = (Godot.AcceptDialog) control;
        }

        public string DialogText
        {
            get => GameController.OnGodot ? SceneControl.DialogText : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.DialogText = value;
                }
            }
        }
    }
}
