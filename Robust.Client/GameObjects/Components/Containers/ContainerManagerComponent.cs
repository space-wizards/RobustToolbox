using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public sealed partial class ContainerManagerComponent : SharedContainerManagerComponent
    {
        [ViewVariables]
        private readonly Dictionary<string, IContainer> _containers = new();

        public override T MakeContainer<T>(string id)
        {
            throw new NotSupportedException("Cannot modify containers on the client.");
        }

        public override bool Remove(IEntity entity)
        {
            // TODO: This will probably need relaxing if we want to predict things like inventories.
            throw new NotSupportedException("Cannot modify containers on the client.");
        }

        protected override IEnumerable<IContainer> GetAllContainersImpl()
        {
            return _containers.Values.Where(c => !c.Deleted);
        }

        public override IContainer GetContainer(string id)
        {
            return _containers[id];
        }

        public override bool HasContainer(string id)
        {
            return _containers.ContainsKey(id);
        }

        public override bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            var ret = _containers.TryGetValue(id, out var cont);
            container = cont!;
            return ret;
        }

        /// <inheritdoc />
        public override bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in _containers.Values)
            {
                if (!contain.Deleted && contain.Contains(entity))
                {
                    container = contain;
                    return true;
                }
            }

            container = default;
            return false;
        }

        public override bool ContainsEntity(IEntity entity)
        {
            foreach (var container in _containers.Values)
            {
                if (!container.Deleted && container.Contains(entity))
                {
                    return true;
                }
            }

            return false;
        }

        public override void ForceRemove(IEntity entity)
        {
            throw new NotSupportedException("Cannot modify containers on the client.");
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if(!(curState is ContainerManagerComponentState cast))
                return;

            // Delete now-gone containers.
            List<string>? toDelete = null;
            foreach (var (id, container) in _containers)
            {
                if (!cast.Containers.ContainsKey(id))
                {
                    container.Shutdown();
                    toDelete ??= new List<string>();
                    toDelete.Add(id);
                }
            }

            if (toDelete != null)
            {
                foreach (var dead in toDelete)
                {
                    _containers.Remove(dead);
                }
            }

            // Add new containers and update existing contents.
            foreach (var (id, data) in cast.Containers)
            {

                if (!_containers.TryGetValue(id, out var container))
                {
                    container = new ClientContainer(id, this);
                    _containers.Add(id, container);
                }

                // sync show flag
                container.ShowContents = data.ShowContents;
                container.OccludesLight = data.OccludesLight;

                // Remove gone entities.
                List<IEntity>? toRemove = null;
                foreach (var entity in container.ContainedEntities)
                {
                    if (!data.ContainedEntities.Contains(entity.Uid))
                    {
                        toRemove ??= new List<IEntity>();
                        toRemove.Add(entity);
                    }
                }

                if (toRemove != null)
                {
                    foreach (var goner in toRemove)
                    {
                        container.Remove(goner);
                    }
                }

                // Add new entities.
                foreach (var uid in data.ContainedEntities)
                {
                    var entity = Owner.EntityManager.GetEntity(uid);

                    if (!container.ContainedEntities.Contains(entity))
                    {
                        container.Insert(entity);
                    }
                }
            }
        }

        public override void InternalContainerShutdown(IContainer container)
        {
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            // On shutdown we won't get to process remove events in the containers so this has to be manually done.
            foreach (var container in _containers.Values)
            {
                foreach (var containerEntity in container.ContainedEntities)
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                        new UpdateContainerOcclusionMessage(containerEntity));
                }
            }
        }
    }
}
