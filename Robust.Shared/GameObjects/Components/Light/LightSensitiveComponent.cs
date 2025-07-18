using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Robust.Shared.GameObjects
{

    /// <summary>
    ///     Marks component that receive illumination from <see cref="SharedPointLightComponent"/>.
    /// </summary>
    [RegisterComponent, NetworkedComponent(), Access(typeof(SharedLightSensitiveSystem)), AutoGenerateComponentState(true)]
    public sealed partial class LightSensitiveComponent : Component
    {

        /// <summary>
        ///     Current illumination value as a percentage.
        /// </summary>
        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadOnly)]
        public float LightLevel = 0f;

        /// <summary>
        ///     When this Entity last had its light level evaluated. 
        ///To prevent multiple expensive updates per tick.
        ////// </summary>
        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadOnly)]
        public TimeSpan NextUpdate = TimeSpan.Zero;

    }
}