using SS14.Shared;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBindingManager : IIoCInterface
    {
        bool Enabled { get; set; }

        void Initialize();
        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}
