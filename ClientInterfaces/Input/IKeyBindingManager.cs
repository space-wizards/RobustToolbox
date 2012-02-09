using System;
using SS13_Shared;
using GorgonLibrary.InputDevices;

namespace ClientInterfaces.Input
{
    public interface IKeyBindingManager
    {
        bool Enabled { get; set; }

        void Initialize(Keyboard keyboard);
        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}
