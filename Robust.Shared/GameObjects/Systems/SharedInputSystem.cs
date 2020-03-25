using Robust.Shared.Input;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedInputSystem : EntitySystem
    {
        public abstract ICommandBindMapping BindMap { get; }
    }
}
