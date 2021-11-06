using System.Collections.Generic;
using Robust.Shared.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Handles moving entities between grids as they move around.
    /// </summary>
    internal sealed class SharedGridTraversalSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private Stack<MoveEvent> _queuedEvents = new();
        private HashSet<EntityUid> _handledThisTick = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MoveEvent>((ref MoveEvent ev) => _queuedEvents.Push(ev));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Need to queue because otherwise calling HandleMove during FrameUpdate will lead to prediction issues.
            // TODO: Need to check if that's even still relevant since transform lerping fix?
            ProcessChanges();
        }

        private void ProcessChanges()
        {
            while (_queuedEvents.TryPop(out var moveEvent))
            {
                if (!_handledThisTick.Add(moveEvent.Sender.Uid)) continue;
                HandleMove(ref moveEvent);
            }

            _handledThisTick.Clear();
        }

        private void HandleMove(ref MoveEvent moveEvent)
        {
            var entity = moveEvent.Sender;

            if (entity.Deleted ||
                entity.HasComponent<IMapComponent>() ||
                entity.HasComponent<IMapGridComponent>() ||
                entity.IsInContainer())
            {
                return;
            }

            var transform = entity.Transform;

            if (float.IsNaN(moveEvent.NewPosition.X) || float.IsNaN(moveEvent.NewPosition.Y))
            {
                return;
            }

            var mapPos = moveEvent.NewPosition.ToMapPos(EntityManager);
            (IEntity newParent, GridId newGrid)? dat = default;

            // Change parent if necessary
            if (_mapManager.TryFindGridAt(transform.MapID, mapPos, out var grid) &&
                EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt) &&
                grid.GridEntityId != entity.Uid)
            {
                if (transform.GridID != grid.Index)
                    dat = (newParent: gridEnt, newGrid: grid.Index);
            }
            else
            {
                // Attach them to map / they are on an invalid grid
                if (transform.GridID != GridId.Invalid)
                    dat = (newParent: _mapManager.GetMapEntity(transform.MapID), newGrid: GridId.Invalid);
            }

            if (dat.HasValue)
            {
                var _dat = dat.Value;
                // Apply the angular velocity of the grid to the object leaving the grid, as linear velocity
                if (entity.TryGetComponent<PhysicsComponent>(out var ePhysComp) &&
                        _mapManager.TryGetGrid(transform.GridID, out var oGridMap) &&
                        EntityManager.TryGetEntity(oGridMap.GridEntityId, out var oGrid) &&
                        oGrid.TryGetComponent<PhysicsComponent>(out var gPhysComp))
                {
                    // Our rotation system is backwards, so we invert the angular velocity.
                    var o = -gPhysComp.MapAngularVelocity;

                    // Get the vector between the grid and the entity leaving
                    var r = oGrid.Transform.WorldPosition - transform.WorldPosition;

                    // Get the tangent of r by rotating it π/2 rad (90°)
                    var v = new Angle(MathHelper.PiOver2).RotateVec(r);

                    // Scale the new vector by the angular velocity
                    v *= o;

                    // Smack it on to the entity
                    ePhysComp.LinearVelocity += v;
                }

                transform.AttachParent(_dat.newParent);
                RaiseLocalEvent(entity.Uid, new ChangedGridEvent(entity, transform.GridID, _dat.newGrid));
            }
        }
    }

    public sealed class ChangedGridEvent : EntityEventArgs
    {
        public IEntity Entity;
        public GridId OldGrid;
        public GridId NewGrid;

        public ChangedGridEvent(IEntity entity, GridId oldGrid, GridId newGrid)
        {
            Entity = entity;
            OldGrid = oldGrid;
            NewGrid = newGrid;
        }
    }
}
