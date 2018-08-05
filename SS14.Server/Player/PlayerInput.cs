using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Input;

namespace SS14.Server.Player
{
    public class PlayerInput : IPlayerInput
    {
        private Dictionary<BoundKeyFunction, (BoundKeyState State, InputCmdHandler Command)> functions
          = new Dictionary<BoundKeyFunction, (BoundKeyState State, InputCmdHandler Command)>();

        private void EnsureFunction(BoundKeyFunction function)
        {
            if (!functions.ContainsKey(function))
            {
                functions[function] = (BoundKeyState.Up, null);
            }
        }

        public InputCmdHandler GetCommand(BoundKeyFunction function)
        {
            if (functions.TryGetValue(function, out var tuple))
            {
                return tuple.Command;
            }
            return null;
        }

        public BoundKeyState GetKeyState(BoundKeyFunction function)
        {
            if (functions.TryGetValue(function, out var tuple))
            {
                return tuple.State;
            }
            return BoundKeyState.Up;
        }

        public bool GetKeyStateBool(BoundKeyFunction function)
        {
            return GetKeyState(function) == BoundKeyState.Down;
        }

        public void SetCommand(BoundKeyFunction function, InputCmdHandler cmdHandler)
        {
            EnsureFunction(function);

            var val = functions[function];
            val.Command = cmdHandler;
            functions[function] = val;
        }

        public void SetFunctionState(BoundKeyFunction function, BoundKeyState state)
        {
            if (state == GetKeyState(function))
            {
                return;
            }

            EnsureFunction(function);

            var val = functions[function];
            val.State = state;
            functions[function] = val;

            if (state == BoundKeyState.Up)
            {
                val.Command?.Disabled();
            }
            else
            {
                val.Command?.Enabled();
            }
        }
    }
}
