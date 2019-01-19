namespace SS14.Client.Graphics
{
    internal class DisplayManagerGodot : DisplayManager
    {
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
