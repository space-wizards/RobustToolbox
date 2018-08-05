using SS14.Shared;
using SS14.Shared.IoC;
using System;
using SS14.Client.Input;
using SS14.Shared.Maths;
using SS14.Shared.Input;

namespace SS14.Client.Interfaces.Input
{
    /// <summary>
    ///     Manages key bindings, input commands and other misc. input systems.
    /// </summary>
    public interface IInputManager
    {
        bool Enabled { get; set; }

        Vector2 MouseScreenPosition { get; }

        BoundKeyMap NetworkBindMap { get; }

        void Initialize();

        void KeyDown(KeyEventArgs e);
        void KeyUp(KeyEventArgs e);

        /// <summary>
        ///     Gets a key binding according to the function it is bound to.
        /// </summary>
        /// <param name="function">The function the key binding is bound to.</param>
        /// <returns>The key binding.</returns>
        IKeyBinding GetKeyBinding(BoundKeyFunction function);
        bool TryGetKeyBinding(BoundKeyFunction function, out IKeyBinding binding);

        /// <summary>
        ///     Returns the input command bound to a key function.
        /// </summary>
        /// <param name="function">The key function to find the bound input command for.</param>
        /// <returns>An input command, if any. Null if no command is set.</returns>
        InputCmdHandler GetInputCommand(BoundKeyFunction function);

        void SetInputCommand(BoundKeyFunction function, InputCmdHandler cmdHandler);


        event Action<BoundKeyFunction> KeyBindDown;
        event Action<BoundKeyFunction> KeyBindUp;
        event Action<BoundKeyEventArgs> KeyBindStateChanged;
    }
}
