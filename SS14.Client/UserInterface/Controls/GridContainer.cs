using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.GridContainer))]
    public class GridContainer : Control
    {
        public GridContainer() : base()
        {
        }

        public GridContainer(string name) : base(name)
        {
        }

        internal GridContainer(Godot.GridContainer sceneControl) : base(sceneControl)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.GridContainer();
        }

        public int Columns
        {
            get => GameController.OnGodot ? (int)SceneControl.Get("columns") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("columns", value);
                }
            }
        }
    }
}
