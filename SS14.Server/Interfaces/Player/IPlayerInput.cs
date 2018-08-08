using SS14.Shared.Input;

namespace SS14.Server.Interfaces.Player
{
    public interface IPlayerInput
    {
        BoundKeyState GetKeyState(BoundKeyFunction function);
        bool GetKeyStateBool(BoundKeyFunction function);

        InputCmdHandler GetCommand(BoundKeyFunction function);
        void SetCommand(BoundKeyFunction function, InputCmdHandler cmdHandler);
    }
}
