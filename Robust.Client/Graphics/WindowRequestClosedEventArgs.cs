namespace Robust.Client.Graphics
{
    public readonly struct WindowRequestClosedEventArgs
    {
        public IClydeWindow Window { get; }

        public WindowRequestClosedEventArgs(IClydeWindow window)
        {
            Window = window;
        }
    }
}
