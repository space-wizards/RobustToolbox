namespace Robust.Client.Graphics
{
    public sealed class WindowCreateParameters
    {
        public int Width = 1280;
        public int Height = 720;
        public string Title = "";
        public bool Maximized;
        public bool Visible = true;
        public IClydeMonitor? Monitor;
        public bool Fullscreen;
    }
}
