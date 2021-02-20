using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    public class ClientContainerSystem : ContainerSystem
    {
        private readonly HashSet<IEntity> _updateQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UpdateContainerOcclusionMessage>(UpdateContainerOcclusion);

            UpdatesBefore.Add(typeof(SpriteSystem));
        }

        private void UpdateContainerOcclusion(UpdateContainerOcclusionMessage ev)
        {
            _updateQueue.Add(ev.Entity);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var toUpdate in _updateQueue)
            {
                if (toUpdate.Deleted)
                {
                    continue;
                }

                UpdateEntityRecursively(toUpdate);
            }

            _updateQueue.Clear();
        }

        private static void UpdateEntityRecursively(IEntity entity)
        {
            // TODO: Since we are recursing down,
            // we could cache ShowContents data here to speed it up for children.
            // Am lazy though.
            UpdateEntity(entity);

            foreach (var child in entity.Transform.Children)
            {
                UpdateEntityRecursively(child.Owner);
            }
        }

        private static void UpdateEntity(IEntity entity)
        {
            if (entity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.ContainerOccluded = false;

                // We have to recursively scan for containers upwards in case of nested containers.
                var tempParent = entity;
                while (tempParent.TryGetContainer(out var container))
                {
                    if (!container.ShowContents)
                    {
                        sprite.ContainerOccluded = true;
                        break;
                    }

                    tempParent = container.Owner;
                }
            }

            if (entity.TryGetComponent(out PointLightComponent? light))
            {
                light.ContainerOccluded = false;

                // We have to recursively scan for containers upwards in case of nested containers.
                var tempParent = entity;
                while (tempParent.TryGetContainer(out var container))
                {
                    if (container.OccludesLight)
                    {
                        light.ContainerOccluded = true;
                        break;
                    }

                    tempParent = container.Owner;
                }
            }
        }
    }
}
