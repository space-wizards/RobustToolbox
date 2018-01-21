using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    class OptionsMenu : SS14Window
    {
        private static readonly Vector2i[] AvailableResolutions = new Vector2i[]
        {
            // 16:9
            new Vector2i(1024, 576),
            new Vector2i(1152, 648),
            new Vector2i(1280, 720),
            new Vector2i(1366, 768),
            new Vector2i(1600, 900),
            new Vector2i(1920, 1080),
            new Vector2i(2560, 1440),
            new Vector2i(3840, 2160),

            // 16:10
            new Vector2i(1280, 800),
            new Vector2i(1440, 900),
            new Vector2i(1680, 1050),
            new Vector2i(1920, 1200),
            new Vector2i(2560, 1600),

            // 4:3
            new Vector2i(640, 480),
            new Vector2i(800, 600),
            new Vector2i(960, 720),
            new Vector2i(1024, 768),
            new Vector2i(1280, 960),
            new Vector2i(1400, 1050),
            new Vector2i(1440, 1080),
            new Vector2i(1600, 1200),
            new Vector2i(1856, 1392),
            new Vector2i(1920, 1440),
            new Vector2i(2048, 1536),
        };

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/OptionsMenu/OptionsMenu.tscn");
            return (Godot.Control)res.Instance();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var options = Contents.GetChild("VBoxContainer").GetChild("ResolutionContainer").GetChild<OptionButton>("ResolutionOption");
            // TODO: Multi monitor support.
            var screenSize = (Vector2i)Godot.OS.GetScreenSize().Convert();

            for (var i = 0; i < AvailableResolutions.Length; i++)
            {
                var res = AvailableResolutions[i];
                if (res.X > screenSize.X || res.Y > screenSize.Y)
                {
                    continue;
                }
                options.AddItem($"{res.X}x{res.Y}", i);
            }
        }
    }
}
