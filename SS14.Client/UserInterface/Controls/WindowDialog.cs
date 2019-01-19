using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.WindowDialog))]
    public class WindowDialog : Popup
    {
        public WindowDialog() : base()
        {
        }

        public WindowDialog(string name) : base(name)
        {
        }

        internal WindowDialog(Godot.WindowDialog control) : base(control)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.WindowDialog();
        }

        public string Title
        {
            get => GameController.OnGodot ? (string)SceneControl.Get("window_title") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("window_title", value);
                }
            }
        }

        public bool Resizable
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("resizable") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("resizable", value);
                }
            }
        }
    }
}
