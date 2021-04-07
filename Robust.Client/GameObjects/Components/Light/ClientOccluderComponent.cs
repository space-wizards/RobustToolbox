using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    internal sealed class ClientOccluderComponent : OccluderComponent
    {
        internal SnapGridComponent? SnapGrid { get; private set; }

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

            if (Owner.TryGetComponent(out SnapGridComponent? snap))
            {
                SnapGrid = snap;
                SnapGrid.OnPositionChanged += SnapGridOnPositionChanged;

                SnapGridOnPositionChanged();
            }
        }

        private void SnapGridOnPositionChanged()
        {
            SendDirty();
            _lastPosition = (Owner.Transform.GridID, SnapGrid!.Position);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if (SnapGrid != null)
            {
                SnapGrid.OnPositionChanged -= SnapGridOnPositionChanged;
            }

            SendDirty();
        }

        private void SendDirty()
        {
            if (SnapGrid != null)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderDirtyEvent(Owner, _lastPosition));
            }
        }

        internal void Update()
        {
            Occluding = OccluderDir.None;

            if (Deleted || SnapGrid == null)
            {
                return;
            }

            void CheckDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in SnapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out ClientOccluderComponent? comp) && comp.Enabled)
                    {
                        Occluding |= oclDir;
                        break;
                    }
                }
            }

            CheckDir(Direction.North, OccluderDir.North);
            CheckDir(Direction.East, OccluderDir.East);
            CheckDir(Direction.South, OccluderDir.South);
            CheckDir(Direction.West, OccluderDir.West);
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
