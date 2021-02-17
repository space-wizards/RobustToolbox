using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects.Components
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
        [YamlField("context")]
        public string ContextName { get; set; } = InputContextContainer.DefaultContextName;
    }
}
