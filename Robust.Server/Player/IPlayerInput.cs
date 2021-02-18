using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Robust.Server.Player
{
    public interface IPlayerInput
    {
        BoundKeyState GetKeyState(BoundKeyFunction function);
        bool GetKeyStateBool(BoundKeyFunction function);

        InputCmdHandler GetCommand(BoundKeyFunction function);
        void SetCommand(BoundKeyFunction function, InputCmdHandler cmdHandler);
    }
}
