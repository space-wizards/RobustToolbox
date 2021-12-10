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

        internal IEnumerable<EntityUid> Entities
        {
            get
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                foreach (var entity in _entities)
                {
                    if (!entMan.Deleted(entity))
                    {
                        yield return entity;
                    }

                    if (!entMan.TryGetComponent(entity, out ContainerManagerComponent? containerManager))
                        continue;

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

        private readonly HashSet<EntityUid> _entities = new();

        internal GridTileLookupNode(GridTileLookupChunk parentChunk, Vector2i indices)
        {
            ParentChunk = parentChunk;
            Indices = indices;
        }

        internal void AddEntity(EntityUid entity)
        {
            _entities.Add(entity);
        }

        internal void RemoveEntity(EntityUid entity)
        {
            _entities.Remove(entity);
        }
    }
}
