using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Analyzer
{
    /// <summary>
    /// A do-nothing <see cref="IEntityManager"/>, just for dependency injection.
    ///
    /// Should never be invoked, because the analyzer does no entity creation.
    /// </summary>
    internal class AnalyzerEntityManager : IEntityManager
    {
        public GameTick CurrentTick => throw new NotImplementedException();

        public IComponentManager ComponentManager => throw new NotImplementedException();

        public IEntityNetworkManager EntityNetManager => throw new NotImplementedException();

        public IEventBus EventBus => throw new NotImplementedException();

        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEntity CreateEntityUninitialized(string prototypeName)
        {
            throw new NotImplementedException();
        }

        public IEntity CreateEntityUninitialized(string prototypeName, GridCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public IEntity CreateEntityUninitialized(string prototypeName, MapCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public void DeleteEntity(IEntity e)
        {
            throw new NotImplementedException();
        }

        public void DeleteEntity(EntityUid uid)
        {
            throw new NotImplementedException();
        }

        public bool EntityExists(EntityUid uid)
        {
            throw new NotImplementedException();
        }

        public void FrameUpdate(float frameTime)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntities(IEntityQuery query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntities()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesInArc(GridCoordinates coordinates, float range, Angle direction, float arcWidth, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float range, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float range, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(GridCoordinates position, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, bool approximate = false)
        {
            throw new NotImplementedException();
        }

        public IEntity GetEntity(EntityUid uid)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public bool RemoveFromEntityTree(IEntity entity, MapId mapId)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public IEntity SpawnEntity(string protoName, GridCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public IEntity SpawnEntity(string protoName, MapCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public IEntity SpawnEntityNoMapInit(string protoName, GridCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public void Startup()
        {
            throw new NotImplementedException();
        }

        public bool TryGetEntity(EntityUid uid, out IEntity entity)
        {
            throw new NotImplementedException();
        }

        public void Update(float frameTime)
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public bool UpdateEntityTree(IEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
