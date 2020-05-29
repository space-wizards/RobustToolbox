using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedInputSystem : EntitySystem
    {
        public abstract ICommandBindRegistry BindRegistry { get; }
    }
}
