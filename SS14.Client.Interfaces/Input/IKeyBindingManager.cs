using GorgonLibrary.InputDevices;
using SS14.Shared;
using System;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBindingManager
    {
        bool Enabled { get; set; }

        void Initialize(Keyboard keyboard);
        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}