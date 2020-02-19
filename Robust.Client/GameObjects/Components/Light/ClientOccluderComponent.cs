using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    internal sealed class ClientOccluderComponent : OccluderComponent
    {
        private SnapGridComponent _snapGrid;

        [ViewVariables]
        private readonly ClientOccluderComponent[] _neighbors = new ClientOccluderComponent[4];

        protected override void Startup()
        {
            base.Startup();

            if (Owner.TryGetComponent(out _snapGrid))
            {
                _snapGrid.OnPositionChanged += SnapGridOnOnPositionChanged;
            }

            UpdateConnections(true);
        }

        private void SnapGridOnOnPositionChanged()
        {
            Disconnect();
            UpdateConnections(true);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if (_snapGrid != null)
            {
                _snapGrid.OnPositionChanged -= SnapGridOnOnPositionChanged;
            }
        }

        private void Disconnect()
        {
            foreach (var neighbor in _neighbors)
            {
                neighbor.UpdateConnections(false);
            }
        }

        private void UpdateConnections(bool propagate)
        {
            _neighbors[0] = _neighbors[1] = _neighbors[2] = _neighbors[3] = null;

            if (Deleted || _snapGrid == null)
            {
                return;
            }

            void CheckDir(Direction dir, OccluderDir oclDir)
            {
                foreach (var neighbor in _snapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out ClientOccluderComponent comp))
                    {
                        _neighbors[(int)oclDir] = comp;
                        if (propagate)
                        {
                            comp.UpdateConnections(false);
                        }
                        break;
                    }
                }
            }

            CheckDir(Direction.North, OccluderDir.North);
            CheckDir(Direction.East, OccluderDir.East);
            CheckDir(Direction.South, OccluderDir.South);
            CheckDir(Direction.West, OccluderDir.West);
        }

        internal bool IsOccluding(OccluderDir dir)
        {
            return _neighbors[(int) dir] == null;
        }

        internal enum OccluderDir : byte
        {
            North = 0,
            East = 1,
            South = 2,
            West = 3,
        }
    }
}
