namespace SS14.Client.UserInterface
{
    public class EscapeMenu : SS14Window
    {
        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/EscapeMenu/EscapeMenu.tscn");
            return (Godot.Control)res.Instance();
        }

        protected override void Initialize()
        {
            base.Initialize();

            Resizable = false;
            HideOnClose = true;
        }
    }
}
