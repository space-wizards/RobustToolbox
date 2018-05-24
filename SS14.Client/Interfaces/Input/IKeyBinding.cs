using SS14.Client.Input;
using SS14.Shared.Input;

namespace SS14.Client.Interfaces.Input
{
    public interface IKeyBinding
    {
        BoundKeyState State { get; }
        BoundKeyFunction Function { get; }
        KeyBindingType BindingType { get; }
    }
}
