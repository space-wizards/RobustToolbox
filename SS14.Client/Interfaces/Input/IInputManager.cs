using SS14.Shared;
using SS14.Shared.IoC;
using System;
using SS14.Client.Graphics.Input;
using SS14.Client.Input;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Input
{
    public interface IInputManager
    {
        bool Enabled { get; set; }

        Vector2 MouseScreenPosition { get; }

        void Initialize();

        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);

        event EventHandler<BoundKeyEventArgs> BoundKeyDown;
        event EventHandler<BoundKeyEventArgs> BoundKeyUp;
    }
}
