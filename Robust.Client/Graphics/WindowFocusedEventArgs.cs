namespace Robust.Client.Graphics
{
    public readonly struct WindowFocusedEventArgs
    {
        public WindowFocusedEventArgs(bool focused, IClydeWindow window)
        {
            Focused = focused;
            Window = window;
        }

        public bool Focused { get; }
        public IClydeWindow Window { get; }
    }
}
