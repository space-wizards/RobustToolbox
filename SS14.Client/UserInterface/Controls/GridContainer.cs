
namespace SS14.Client.UserInterface.Controls
{
    public class GridContainer : Control
    {
        public GridContainer() : base() { }
        public GridContainer(string name) : base(name) { }
        public GridContainer(Godot.GridContainer sceneControl) : base(sceneControl) { }

        new private Godot.GridContainer SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.GridContainer();
        }

        public int Columns
        {
            get => SceneControl.GetColumns();
            set => SceneControl.SetColumns(value);
        }
    }
}
