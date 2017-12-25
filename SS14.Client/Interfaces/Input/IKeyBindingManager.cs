using SS14.Shared;
using SS14.Shared.IoC;
using System;
using SS14.Client.Input;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBindingManager
    {
        bool Enabled { get; set; }

        void Initialize();

        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);

        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}
