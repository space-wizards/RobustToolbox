using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Storage for physics fixtures
    /// </summary>
    /// <remarks>
    /// In its own component to decrease physics comp state size significantly.
    /// </remarks>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FixturesComponent : Component
    {
        // This is a snowflake component whose main job is making physics states smaller for massive bodies
        // (e.g. grids)
        // Content generally shouldn't care about its existence.

        [ViewVariables]
        public int FixtureCount => Fixtures.Count;

        /// <summary>
        /// Allows us to reference a specific fixture when we contain multiple
        /// This is useful for stuff like slippery objects that might have a non-hard layer for mob collisions and
        /// a hard layer for wall collisions.
        /// <remarks>
        /// We can also use this for networking to make cross-referencing fixtures easier.
        /// Won't call Dirty() by default
        /// </remarks>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("fixtures", customTypeSerializer:typeof(FixtureSerializer))]
        [NeverPushInheritance]
        [Access(typeof(FixtureSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public Dictionary<string, Fixture> Fixtures = new();
    }
}
