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

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.AcceptDialog();
        }

        public string DialogText
        {
            get => GameController.OnGodot ? (string)SceneControl.Get("dialog_text") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("dialog_text", value);
                }
            }
        }
    }
}
