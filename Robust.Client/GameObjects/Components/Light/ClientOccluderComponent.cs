using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [ComponentReference(typeof(OccluderComponent))]
    public sealed class ClientOccluderComponent : OccluderComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private (GridId, Vector2i) _lastPosition;
        [ViewVariables] internal OccluderDir Occluding { get; private set; }
        [ViewVariables] internal uint UpdateGeneration { get; set; }

        public override bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;

                SendDirty();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (_entityManager.GetComponent<TransformComponent>(Owner).Anchored)
            {
                AnchorStateChanged();
            }
        }

        public void AnchorStateChanged()
        {
            var xform = _entityManager.GetComponent<TransformComponent>(Owner);
            SendDirty(xform);

            if(!xform.Anchored)
                return;

            var grid = _mapManager.GetGrid(xform.GridID);
            _lastPosition = (xform.GridID, grid.TileIndicesFor(xform.Coordinates));
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            SendDirty();
        }

        private void SendDirty(TransformComponent? xform = null)
        {
            xform ??= _entityManager.GetComponent<TransformComponent>(Owner);
            if (xform.Anchored)
            {
                _entityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderDirtyEvent(Owner, _lastPosition));
            }
        }

        internal void Update()
        {
            Occluding = OccluderDir.None;

            if (Deleted)
                return;

            // Content may want to override the default behavior for occlusion.
            var xform = _entityManager.GetComponent<TransformComponent>(Owner);
            var ev = new OccluderDirectionsEvent
            {
                Component = xform,
            };

            _entityManager.EventBus.RaiseLocalEvent(Owner, ref ev);

            if (ev.Handled)
            {
                Occluding = ev.Directions;
                return;
            }

            if (!xform.Anchored)
                return;

            var grid = _mapManager.GetGrid(xform.GridID);
            var position = xform.Coordinates;
            void CheckDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in grid.GetInDir(position, dir))
                {
                    if (_entityManager.TryGetComponent(neighbor, out ClientOccluderComponent? comp) && comp.Enabled)
                    {
                        Occluding |= oclDir;
                        break;
                    }
                }
            }

            var angle = xform.LocalRotation;
            var dirRolling = angle.GetCardinalDir();
            // dirRolling starts at effective south

            CheckDir(dirRolling, OccluderDir.South);
            dirRolling = dirRolling.GetClockwise90Degrees();

            CheckDir(dirRolling, OccluderDir.West);
            dirRolling = dirRolling.GetClockwise90Degrees();

            CheckDir(dirRolling, OccluderDir.North);
            dirRolling = dirRolling.GetClockwise90Degrees();

            CheckDir(dirRolling, OccluderDir.East);
        }
    }

    [Flags]
    public enum OccluderDir : byte
    {
        None = 0,
        North = 1,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
    }

    /// <summary>
    /// Raised by occluders when trying to get occlusion directions.
    /// </summary>
    [ByRefEvent]
    public struct OccluderDirectionsEvent
    {
        public bool Handled = false;
        public OccluderDir Directions = OccluderDir.None;
        public TransformComponent Component = default!;
    }
}
