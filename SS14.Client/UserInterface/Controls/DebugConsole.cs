using SS14.Shared.Log;
using SS14.Shared.Reflection;

namespace SS14.Client.UserInterface
{
    // Disable reflection so that we won't be looked at for scene translation.
    [Reflect(false)]
    public class DebugConsole : Control
    {
        private LineEdit CommandBar;
        private Control LogContainer;

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/DebugConsole/DebugConsole.tscn");
            var node = (Godot.Control)res.Instance();
            node.Visible = false;
            return node;
        }

        protected override void Initialize()
        {
            CommandBar = GetChild<LineEdit>("CommandBar");
            LogContainer = GetChild("ScrollContents").GetChild("VBoxContainer");

            CommandBar.OnTextEntered += CommandEntered;
        }

        public void Toggle()
        {
            Visible = !Visible;
            if (Visible)
            {
                CommandBar.GrabFocus();
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                return;
            }
            var newtext = new Label();
            newtext.Text = args.Text;
            LogContainer.AddChild(newtext);

            CommandBar.Clear();
        }
    }
}
