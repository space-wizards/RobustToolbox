using Robust.Client.Input;
using Robust.Shared.Input;

namespace Robust.Client.Interfaces.Input
{
    public interface IKeyBinding
    {
        BoundKeyState State { get; }
        BoundKeyFunction Function { get; }
        KeyBindingType BindingType { get; }
    }
}
