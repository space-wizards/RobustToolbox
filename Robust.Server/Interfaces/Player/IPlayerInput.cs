using Robust.Shared.Input;

namespace Robust.Server.Interfaces.Player
{
    public interface IPlayerInput
    {
        BoundKeyState GetKeyState(BoundKeyFunction function);
        bool GetKeyStateBool(BoundKeyFunction function);

        InputCmdHandler GetCommand(BoundKeyFunction function);
        void SetCommand(BoundKeyFunction function, InputCmdHandler cmdHandler);
    }
}
