using System;

namespace SS14.Client.Graphics.Input
{
    public class TextEventArgs : EventArgs
    {
        public string Text { get; }

        public TextEventArgs(string text)
        {
            Text = text;
        }

        public static explicit operator TextEventArgs(SFML.Window.TextEventArgs args)
        {
            return new TextEventArgs(args.Unicode);
        }
    }
}
