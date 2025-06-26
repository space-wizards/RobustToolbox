using System;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Placement
{
    public interface IPlacementManager
    {
        void Initialize();
        bool IsActive { get; }
        bool Eraser { get; }

        /// <summary>
        /// Do we replace any existing entities with matching keys.
        /// </summary>
        bool Replacement { get; set; }
        PlacementMode? CurrentMode { get; set; }
        PlacementInformation? CurrentPermission { get; set; }

        IEntityManager EntityManager { get; }
        IEyeManager EyeManager { get; }
        IMapManager MapManager { get; }

        /// <summary>
        /// The direction to spawn the entity in (presently exposed for EntitySpawnWindow UI)
        /// </summary>
        Direction Direction { get; set; }

        /// <summary>
        /// Whether a tile placement should be mirrored or not.
        /// </summary>
        bool Mirrored { get; set; }

        /// <summary>
        /// Gets called when Direction changed (presently for EntitySpawnWindow/TileSpawnWindow UI)
        /// </summary>
        event EventHandler DirectionChanged;

        /// <summary>
        /// Gets called when Mirrored changed (presently for TileSpawnWindow UI)
        /// </summary>
        event EventHandler MirroredChanged;

        /// <summary>
        /// Gets called when the PlacementManager changed its build/erase mode or when the hijacks changed
        /// </summary>
        event EventHandler PlacementChanged;

        void BeginPlacing(PlacementInformation info, PlacementHijack? hijack = null);
        void Clear();
        void ToggleEraser();
        void ToggleEraserHijacked(PlacementHijack hijack);

        void FrameUpdate(FrameEventArgs e);
    }
}
