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

        new private Godot.GridContainer SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.GridContainer();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.GridContainer) control;
        }

        public int Columns
        {
            get => GameController.OnGodot ? SceneControl.GetColumns() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetColumns(value);
                }
            }
        }
    }
}
