namespace SS14.Client.UserInterface.Controls
{
    public class AcceptDialog : WindowDialog
    {
        public AcceptDialog() : base()
        {
        }
        public AcceptDialog(string name) : base(name)
        {
        }
        public AcceptDialog(Godot.AcceptDialog control) : base(control)
        {
        }

        new private Godot.AcceptDialog SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.AcceptDialog();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.AcceptDialog)control;
        }

        public string DialogText
        {
            get => SceneControl.DialogText;
            set => SceneControl.DialogText = value;
        }
    }
}
