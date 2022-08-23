namespace Robust.Client.WebView
{
    public sealed class BrowserWindowCreateParameters
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public string Url { get; set; } = "about:blank";

        public BrowserWindowCreateParameters(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
