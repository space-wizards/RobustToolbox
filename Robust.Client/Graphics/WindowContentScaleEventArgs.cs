namespace Robust.Client.Graphics
{
    public readonly struct WindowContentScaleEventArgs
    {
        public WindowContentScaleEventArgs(IClydeWindow window)
        {
            Window = window;
        }

        public IClydeWindow Window { get; }
    }
}
