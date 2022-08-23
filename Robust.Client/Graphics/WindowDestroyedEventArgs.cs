namespace Robust.Client.Graphics
{
    public readonly struct WindowDestroyedEventArgs
    {
        public IClydeWindow Window { get; }

        public WindowDestroyedEventArgs(IClydeWindow window)
        {
            Window = window;
        }
    }
}
