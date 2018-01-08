using SS14.Server.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using SS14.Shared.ContentPack;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ServerEntityManager : EntityManager, IServerEntityManager
    {
        #region IEntityManager Members

        [Dependency]
        readonly private IPrototypeManager _protoManager;

        /// <inheritdoc />
        public bool TrySpawnEntityAt(string EntityType, LocalCoordinates coordinates, out IEntity entity)
        {
            var prototype = _protoManager.Index<EntityPrototype>(EntityType);
            if (prototype.CanSpawnAt(coordinates.Grid, coordinates.Position))
            {
                entity = SpawnEntity(EntityType);
                entity.GetComponent<TransformComponent>().LocalPosition = coordinates;
                entity.Initialize();
                return true;
            }
            entity = null;
            return false;
        }

        /// <inheritdoc />
        public bool TrySpawnEntityAt(string EntityType, Vector2 position, int argMap, out IEntity entity)
        {
            var mapmanager = IoCManager.Resolve<IMapManager>();
            var coordinates = new LocalCoordinates(position, mapmanager.GetMap(argMap).FindGridAt(position));
            return TrySpawnEntityAt(EntityType, coordinates, out entity);
        }

        /// <inheritdoc />
        public IEntity ForceSpawnEntityAt(string EntityType, LocalCoordinates coordinates)
        {
            IEntity entity = SpawnEntity(EntityType);
            entity.GetComponent<TransformComponent>().LocalPosition = coordinates;
            entity.Initialize();
            return entity;
        }

        /// <inheritdoc />
        public IEntity ForceSpawnEntityAt(string entityType, Vector2 position, int argMap)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetMap(argMap, out var map))
            {
                map = mapManager.DefaultMap;
            }

            return ForceSpawnEntityAt(entityType, new LocalCoordinates(position, map.FindGridAt(position)));
        }

        public List<EntityState> GetEntityStates()
        {
            var stateEntities = new List<EntityState>();
            foreach (IEntity entity in GetEntities())
            {
                EntityState entityState = entity.GetEntityState();
                stateEntities.Add(entityState);
            }
            return stateEntities;
        }

        #endregion IEntityManager Members

        public void Initialize()
        {
            EntitySystemManager.Initialize();
            Initialized = true;
            InitializeEntities();
        }
    }
}
