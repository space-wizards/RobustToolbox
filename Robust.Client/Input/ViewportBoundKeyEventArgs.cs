using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Robust.Client.Input
{
    public sealed class ViewportBoundKeyEventArgs
    {
        public BoundKeyEventArgs KeyEventArgs { get; }
        public Control? Viewport { get; }

        public ViewportBoundKeyEventArgs(BoundKeyEventArgs keyEventArgs, Control? viewport)
        {
            KeyEventArgs = keyEventArgs;
            Viewport = viewport;
        }
    }
}
