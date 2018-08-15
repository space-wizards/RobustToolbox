using SS14.Client.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Serialization;

namespace SS14.Client.GameObjects.Components
{
    /// <summary>
    ///     Defines data fields used in the <see cref="InputSystem"/>.
    /// </summary>
    class InputComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Input";

        /// <summary>
        ///     The context that will be made active for a client that attaches to this entity.
        /// </summary>
        public string ContextName { get; set; }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("context", InputContextContainer.DefaultContextName, value => ContextName = value, () => ContextName);
        }
    }
}
