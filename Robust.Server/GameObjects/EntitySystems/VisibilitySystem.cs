using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public sealed class VisibilitySystem : EntitySystem
    {
        [Dependency] private readonly PvsSystem _pvs = default!;
        [Dependency] private readonly IViewVariablesManager _vvManager = default!;

        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<VisibilityComponent> _visiblityQuery;

        public override void Initialize()
        {
            base.Initialize();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            _visiblityQuery = GetEntityQuery<VisibilityComponent>();
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            EntityManager.EntityInitialized += OnEntityInit;

            _vvManager.GetTypeHandler<VisibilityComponent>()
                .AddPath(nameof(VisibilityComponent.Layer), (_, comp) => comp.Layer, (uid, value, comp) =>
                {
                    if (!Resolve(uid, ref comp))
                        return;

                    SetLayer(uid, comp, value);
                });
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }

        public void AddLayer(EntityUid uid, VisibilityComponent component, int layer, bool refresh = true)
        {
            if ((layer & component.Layer) == layer)
                return;

            component.Layer |= layer;

            if (refresh)
                RefreshVisibility(uid, visibilityComponent: component);
        }

        public void RemoveLayer(EntityUid uid, VisibilityComponent component, int layer, bool refresh = true)
        {
            if ((layer & component.Layer) != layer)
                return;

            component.Layer &= ~layer;

            if (refresh)
                RefreshVisibility(uid, visibilityComponent: component);
        }

        public void SetLayer(EntityUid uid, VisibilityComponent component, int layer, bool refresh = true)
        {
            if (component.Layer == layer)
                return;

            component.Layer = layer;

            if (refresh)
                RefreshVisibility(uid, visibilityComponent: component);
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            RefreshVisibility(ev.Entity);
        }

        private void OnEntityInit(EntityUid uid)
        {
            RefreshVisibility(uid);
        }

        public void RefreshVisibility(EntityUid uid,
            VisibilityComponent? visibilityComponent = null,
            MetaDataComponent? meta = null)
        {
            if (!_metaQuery.Resolve(uid, ref meta, false))
                return;

            // Iterate up through parents and calculate the cumulative visibility mask.
            var mask = GetParentVisibilityMask(uid, visibilityComponent);

            // Iterate down through children and propagate mask changes.
            RecursivelyApplyVisibility(uid, mask, meta);
        }

        private void RecursivelyApplyVisibility(EntityUid uid, int mask, MetaDataComponent meta)
        {
            if (meta.VisibilityMask == mask)
                return;

            var xform = _xformQuery.GetComponent(uid);
            meta.VisibilityMask = mask;
            _pvs.MarkDirty(uid, xform);

            foreach (var child in xform.ChildEntities)
            {
                if (!_metaQuery.TryGetComponent(child, out var childMeta))
                    continue;

                var childMask = mask;

                if (_visiblityQuery.TryGetComponent(child, out VisibilityComponent? hildVis))
                    childMask |= hildVis.Layer;

                RecursivelyApplyVisibility(child, childMask, childMeta);
            }
        }

        private int GetParentVisibilityMask(EntityUid uid, VisibilityComponent? visibilityComponent = null)
        {
            int visMask = 1; // apparently some content expects everything to have the first bit/flag set to true.
            if (_visiblityQuery.Resolve(uid, ref visibilityComponent, false))
                visMask |= visibilityComponent.Layer;

            // Include parent vis masks
            if (_xformQuery.TryGetComponent(uid, out var xform) && xform.ParentUid.IsValid())
                visMask |= GetParentVisibilityMask(xform.ParentUid);

            return visMask;
        }
    }
}
