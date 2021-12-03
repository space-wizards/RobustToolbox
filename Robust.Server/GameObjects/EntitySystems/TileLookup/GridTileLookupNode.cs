using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    internal sealed class GridTileLookupNode
    {
        internal GridTileLookupChunk ParentChunk { get; }

        internal Vector2i Indices { get; }

        internal IEnumerable<IEntity> Entities
        {
            get
            {
                foreach (var entity in _entities)
                {
                    if (!((!IoCManager.Resolve<IEntityManager>().EntityExists(entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted))
                    {
                        yield return entity;
                    }

                    if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager)) continue;
                    foreach (var container in containerManager.GetAllContainers())
                    {
                        foreach (var child in container.ContainedEntities)
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        private readonly HashSet<IEntity> _entities = new();

        internal GridTileLookupNode(GridTileLookupChunk parentChunk, Vector2i indices)
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
