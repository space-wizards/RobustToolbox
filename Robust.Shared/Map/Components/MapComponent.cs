using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class MapComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public bool LightingEnabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadOnly), Access(typeof(SharedMapSystem), Other = AccessPermissions.ReadExecute)]
        public MapId MapId { get; internal set; } = MapId.Nullspace;

        [DataField, Access(typeof(SharedMapSystem), typeof(MapManager))]
        public bool MapPaused;

        [DataField, Access(typeof(SharedMapSystem), typeof(MapManager))]
        public bool MapInitialized;
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MapComponentState(MapId mapId, bool lightingEnabled, bool paused, bool init)
        : ComponentState
    {
        public MapId MapId = mapId;
        public bool LightingEnabled = lightingEnabled;
        public bool MapPaused = paused;
        public bool Initialized = init;
    }
}
