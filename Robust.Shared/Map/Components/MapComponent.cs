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
        [DataField("lightingEnabled")]
        public bool LightingEnabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadOnly)]
        public MapId MapId { get; internal set; } = MapId.Nullspace;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool MapPaused { get; set; } = false;

        //TODO replace MapPreInit with the map's entity life stage
        [ViewVariables(VVAccess.ReadOnly)]
        public bool MapPreInit { get; set; } = false;
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MapComponentState : ComponentState
    {
        public MapId MapId;
        public bool LightingEnabled;
        public bool MapPaused;

        public MapComponentState(MapId mapId, bool lightingEnabled, bool paused)
        {
            MapId = mapId;
            LightingEnabled = lightingEnabled;
            MapPaused = paused;
        }
    }
}
