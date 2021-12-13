using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public class VisibilitySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            EntityManager.EntityInitialized += OnEntityInit;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }

        public void AddLayer(VisibilityComponent component, int layer, bool refresh = true)
        {
            if ((layer & component.Layer) == layer) return;
            component.Layer |= layer;

            if (refresh)
                RefreshVisibility(component.Owner);
        }

        public void RemoveLayer(VisibilityComponent component, int layer, bool refresh = true)
        {
            if ((layer & component.Layer) != layer) return;
            component.Layer &= ~layer;

            if (refresh)
                RefreshVisibility(component.Owner);
        }

        public void SetLayer(VisibilityComponent component, int layer, bool refresh = true)
        {
            if (component.Layer == layer) return;
            component.Layer = layer;

            if (refresh)
                RefreshVisibility(component.Owner);
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            RefreshVisibility(ev.Entity);
        }

        private void OnEntityInit(object? sender, EntityUid uid)
        {
            RefreshVisibility(uid);
        }

        public void RefreshVisibility(EntityUid uid)
        {
            if (!EntityManager.TryGetComponent(uid, out MetaDataComponent? metaDataComponent))
            {
                // This means it's deleting or some shit; I'd love to make it a GetComponent<T> in future.
                return;
            }

            var visMask = 1;
            metaDataComponent.VisibilityMask = GetVisibilityMask(uid, ref visMask);
        }

        public void RefreshVisibility(VisibilityComponent visibilityComponent)
        {
            RefreshVisibility(visibilityComponent.Owner);
        }

        private int GetVisibilityMask(EntityUid uid, ref int visMask)
        {
            if (EntityManager.TryGetComponent(uid, out VisibilityComponent visibilityComponent))
            {
                visMask |= visibilityComponent.Layer;
            }

            var xform = EntityManager.GetComponent<TransformComponent>(uid);
            if (xform.ParentUid.IsValid())
            {
                GetVisibilityMask(xform.ParentUid, ref visMask);
            }

            return visMask;
        }
    }
}
