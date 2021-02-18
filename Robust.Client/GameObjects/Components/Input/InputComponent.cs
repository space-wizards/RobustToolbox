using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
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
        [ViewVariables(VVAccess.ReadWrite)]
        public string ContextName { get; set; } = default!;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("context", InputContextContainer.DefaultContextName, value => ContextName = value, () => ContextName);
        }
    }
}
