using System;
using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public sealed class VisibilitySystem : SharedVisibilitySystem
    {
        [Dependency] private readonly PvsSystem _pvs = default!;
        [Dependency] private readonly IViewVariablesManager _vvManager = default!;

        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<VisibilityComponent> _visibilityQuery;

        public override void Initialize()
        {
            base.Initialize();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            _visibilityQuery = GetEntityQuery<VisibilityComponent>();
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            EntityManager.EntityInitialized += OnEntityInit;

            _vvManager.GetTypeHandler<VisibilityComponent>()
                .AddPath(nameof(VisibilityComponent.Layer), (_, comp) => comp.Layer, (uid, value, comp) =>
                {
                    if (!Resolve(uid, ref comp))
                        return;

                    SetLayer((uid, comp), value);
                });
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }

        public override void AddLayer(Entity<VisibilityComponent?> ent, ushort layer, bool refresh = true)
        {
            ent.Comp ??= _visibilityQuery.CompOrNull(ent.Owner) ?? AddComp<VisibilityComponent>(ent.Owner);

            if ((layer & ent.Comp.Layer) == layer)
                return;

            ent.Comp.Layer |= layer;

            if (refresh)
                RefreshVisibility(ent);
        }

        public override void RemoveLayer(Entity<VisibilityComponent?> ent, ushort layer, bool refresh = true)
        {
            if (!_visibilityQuery.Resolve(ent.Owner, ref ent.Comp, false))
                return;

            if ((layer & ent.Comp.Layer) != layer)
                return;

            ent.Comp.Layer &= (ushort)~layer;

            if (refresh)
                RefreshVisibility(ent);
        }

        public override void SetLayer(Entity<VisibilityComponent?> ent, ushort layer, bool refresh = true)
        {
            ent.Comp ??= _visibilityQuery.CompOrNull(ent.Owner) ?? AddComp<VisibilityComponent>(ent.Owner);

            if (ent.Comp.Layer == layer)
                return;

            ent.Comp.Layer = layer;

            if (refresh)
                RefreshVisibility(ent);
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            RefreshVisibility(ev.Entity);
        }

        private void OnEntityInit(Entity<MetaDataComponent> ent)
        {
            RefreshVisibility(ent.Owner, null, ent.Comp);
        }

        public override void RefreshVisibility(EntityUid uid,
            VisibilityComponent? visibilityComponent = null,
            MetaDataComponent? meta = null)
        {
            RefreshVisibility((uid, visibilityComponent, meta));
        }

        public override void RefreshVisibility(Entity<VisibilityComponent?, MetaDataComponent?> ent)
        {
            if (!_metaQuery.Resolve(ent, ref ent.Comp2, false))
                return;

            // Iterate up through parents and calculate the cumulative visibility mask.
            var mask = GetParentVisibilityMask(ent);

            // Iterate down through children and propagate mask changes.
            RecursivelyApplyVisibility(ent.Owner, mask, ent.Comp2);
        }

        private void RecursivelyApplyVisibility(EntityUid uid, ushort mask, MetaDataComponent meta)
        {
            if (meta.VisibilityMask == mask)
                return;

            var xform = _xformQuery.GetComponent(uid);
            meta.VisibilityMask = mask;
            _pvs.SyncMetadata(meta);

            foreach (var child in xform._children)
            {
                if (!_metaQuery.TryGetComponent(child, out var childMeta))
                    continue;

                var childMask = mask;

                if (_visibilityQuery.TryGetComponent(child, out var childVis))
                    childMask |= childVis.Layer;

                RecursivelyApplyVisibility(child, childMask, childMeta);
            }
        }

        private ushort GetParentVisibilityMask(Entity<VisibilityComponent?> ent)
        {
            ushort visMask = 1; // apparently some content expects everything to have the first bit/flag set to true.
            if (_visibilityQuery.Resolve(ent.Owner, ref ent.Comp, false))
                visMask |= ent.Comp.Layer;

            // Include parent vis masks
            if (_xformQuery.TryGetComponent(ent.Owner, out var xform) && xform.ParentUid.IsValid())
                visMask |= GetParentVisibilityMask(xform.ParentUid);

            return visMask;
        }
    }
}
