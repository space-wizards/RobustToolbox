using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    internal class DisplayManagerGodot : DisplayManager
    {
        public override Vector2i ScreenSize => (Vector2i)Godot.OS.WindowSize.Convert();

        public override void SetWindowTitle(string title)
        {
            Godot.OS.SetWindowTitle(title);
        }

        public override void Initialize()
        {
            ReloadConfig();
        }

        public override void ReloadConfig()
        {
            base.ReloadConfig();

            Godot.OS.VsyncEnabled = VSync;
            Godot.OS.WindowFullscreen = WindowMode == WindowMode.Fullscreen;
        }
    }
}
