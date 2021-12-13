using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [ComponentReference(typeof(OccluderComponent))]
    internal sealed class ClientOccluderComponent : OccluderComponent
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
            SendDirty();

            if(!_entityManager.GetComponent<TransformComponent>(Owner).Anchored)
                return;

            var grid = _mapManager.GetGrid(_entityManager.GetComponent<TransformComponent>(Owner).GridID);
            _lastPosition = (_entityManager.GetComponent<TransformComponent>(Owner).GridID, grid.TileIndicesFor(_entityManager.GetComponent<TransformComponent>(Owner).Coordinates));
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            SendDirty();
        }

        private void SendDirty()
        {
            if (_entityManager.GetComponent<TransformComponent>(Owner).Anchored)
            {
                _entityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderDirtyEvent(Owner, _lastPosition));
            }
        }

        internal void Update()
        {
            Occluding = OccluderDir.None;

            if (Deleted || !_entityManager.GetComponent<TransformComponent>(Owner).Anchored)
            {
                return;
            }

            var grid = _mapManager.GetGrid(_entityManager.GetComponent<TransformComponent>(Owner).GridID);
            var position = _entityManager.GetComponent<TransformComponent>(Owner).Coordinates;
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

            var angle = _entityManager.GetComponent<TransformComponent>(Owner).LocalRotation;
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

        [Flags]
        internal enum OccluderDir : byte
        {
            None = 0,
            North = 1,
            East = 1 << 1,
            South = 1 << 2,
            West = 1 << 3,
        }
    }
}
