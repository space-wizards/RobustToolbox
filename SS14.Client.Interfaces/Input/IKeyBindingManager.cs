using SS14.Shared;
using System;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBindingManager
    {
        bool Enabled { get; set; }

        void Initialize();
        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}