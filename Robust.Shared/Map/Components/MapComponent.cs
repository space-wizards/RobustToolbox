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
    public sealed class MapComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
        private MapId _mapIndex = MapId.Nullspace;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(("lightingEnabled"))]
        public bool LightingEnabled { get; set; } = true;

        public MapId WorldMap
        {
            get => _mapIndex;
            internal set => _mapIndex = value;
        }

        [ViewVariables(VVAccess.ReadOnly)]
        public bool MapPaused { get; set; } = false;

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

        public MapComponentState(MapId mapId, bool lightingEnabled)
        {
            MapId = mapId;
            LightingEnabled = lightingEnabled;
        }
    }
}
