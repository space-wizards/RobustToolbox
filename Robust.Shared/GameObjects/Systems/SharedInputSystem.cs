using Robust.Shared.Input.Binding;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedInputSystem : EntitySystem
    {
        private CommandBindRegistry _bindRegistry = default!;

        protected override void PostInject()
        {
            base.PostInject();

            _bindRegistry = new CommandBindRegistry(Log);
        }

        /// <summary>
        ///     Holds the keyFunction -> handler bindings for the simulation.
        /// </summary>
        public ICommandBindRegistry BindRegistry => _bindRegistry;
    }
}
