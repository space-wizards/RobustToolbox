using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects.EntitySystems.TileLookup
{
    internal sealed class GridTileLookupNode
    {
        internal GridTileLookupChunk ParentChunk { get; }
        
        internal MapIndices Indices { get; }

        internal IEnumerable<IEntity> Entities
        {
            get
            {
                foreach (var entity in _entities)
                {
                    if (!entity.Deleted)
                        yield return entity;
                }
            }
        }

        private readonly HashSet<IEntity> _entities = new HashSet<IEntity>();

        internal GridTileLookupNode(GridTileLookupChunk parentChunk, MapIndices indices)
        {
            ParentChunk = parentChunk;
            Indices = indices;
        }
        
        internal void AddEntity(IEntity entity)
        {
            _entities.Add(entity);
        }

        internal void RemoveEntity(IEntity entity)
        {
            _entities.Remove(entity);
        }
    }
}