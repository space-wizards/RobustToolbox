using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Represents a world map inside the ECS system.
    /// </summary>
    public interface IMapComponent : IComponent
    {
        bool LightingEnabled { get; set; }
        Color AmbientLightColor { get; set; }
        MapId WorldMap { get; }
        bool MapPaused { get; internal set; }
        bool MapPreInit { get; internal set; }
    }

    /// <inheritdoc cref="IMapComponent"/>
    [ComponentReference(typeof(IMapComponent))]
    [NetworkedComponent]
    public sealed class MapComponent : Component, IMapComponent
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
        private MapId _mapIndex = MapId.Nullspace;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(("lightingEnabled"))]
        public bool LightingEnabled { get; set; } = true;

        /// <summary>
        /// Ambient light. This is in linear-light, i.e. when providing a fixed colour, you must use Color.FromSrgb(Color.Black)!
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ambientLightColor")]
        public Color AmbientLightColor { get; set; } = Color.FromSrgb(Color.Black);

        /// <inheritdoc />
        public MapId WorldMap
        {
            get => _mapIndex;
            internal set => _mapIndex = value;
        }

        [ViewVariables(VVAccess.ReadOnly)]
        internal bool MapPaused { get; set; } = false;

        /// <inheritdoc />
        bool IMapComponent.MapPaused
        {
            get => this.MapPaused;
            set => this.MapPaused = value;
        }

        [ViewVariables(VVAccess.ReadOnly)]
        internal bool MapPreInit { get; set; } = false;

        /// <inheritdoc />
        bool IMapComponent.MapPreInit
        {
            get => this.MapPreInit;
            set => this.MapPreInit = value;
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MapComponentState : ComponentState
    {
        public MapId MapId;
        public bool LightingEnabled;
        public Color AmbientLightColor;

        public MapComponentState(MapId mapId, bool lightingEnabled, Color ambientLightColor)
        {
            MapId = mapId;
            LightingEnabled = lightingEnabled;
            AmbientLightColor = ambientLightColor;
        }
    }
}
