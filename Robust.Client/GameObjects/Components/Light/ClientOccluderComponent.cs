using System;
using System.Collections.Generic;
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

            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Anchored)
            {
                AnchorStateChanged();
            }
        }

        public void AnchorStateChanged()
        {
            SendDirty();

            if(!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Anchored)
                return;

            var grid = _mapManager.GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).GridID);
            _lastPosition = (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).GridID, grid.TileIndicesFor(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Coordinates));
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            SendDirty();
        }

        private void SendDirty()
        {
            if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Anchored)
            {
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local,
                    new OccluderDirtyEvent(Owner, _lastPosition));
            }
        }

        internal void Update()
        {
            Occluding = OccluderDir.None;

            if (Deleted || !IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Anchored)
            {
                return;
            }

            var grid = _mapManager.GetGrid(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).GridID);
            var position = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Coordinates;
            void CheckDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in grid.GetInDir(position, dir))
                {
                    if (IoCManager.Resolve<IEntityManager>().TryGetComponent(neighbor, out ClientOccluderComponent? comp) && comp.Enabled)
                    {
                        Occluding |= oclDir;
                        break;
                    }
                }
            }

            var angle = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).LocalRotation;
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
