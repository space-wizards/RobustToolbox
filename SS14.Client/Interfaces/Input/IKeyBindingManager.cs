using System;
using SS14.Client.Graphics.Input;
using SS14.Client.Input;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBindingManager
    {
        void Initialize();

        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);

        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}
