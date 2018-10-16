using System;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("GridContainer")]
    public class GridContainer : Control
    {
        public GridContainer() : base() { }
        public GridContainer(string name) : base(name) { }
        #if GODOT
        internal GridContainer(Godot.GridContainer sceneControl) : base(sceneControl) { }

        new private Godot.GridContainer SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.GridContainer();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.GridContainer)control;
        }
        #endif

        public int Columns
        {
            #if GODOT
            get => SceneControl.GetColumns();
            set => SceneControl.SetColumns(value);
            #else
            get => default;
            set { }
            #endif
        }
    }
}
