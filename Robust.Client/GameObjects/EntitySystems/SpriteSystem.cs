using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private RenderingTreeSystem _treeSystem = default!;
        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            _treeSystem = Get<RenderingTreeSystem>();
            SubscribeLocalEvent<SpriteUpdateInertEvent>(QueueUpdateInert);
        }

        private void QueueUpdateInert(SpriteUpdateInertEvent ev)
        {
            _inertUpdateQueue.Enqueue(ev.Sprite);
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            while (_inertUpdateQueue.TryDequeue(out var sprite))
            {
                sprite.DoUpdateIsInert();
            }

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            var pvsBounds = _eyeManager.GetWorldViewport().Enlarged(5);

            var currentMap = _eyeManager.CurrentMap;
            if (currentMap == MapId.Nullspace)
            {
                return;
            }

            foreach (var comp in _treeSystem.GetRenderTrees(currentMap, pvsBounds))
            {
                var bounds = pvsBounds.Translated(-comp.Owner.Transform.WorldPosition);

                comp.SpriteTree.QueryAabb(ref frameTime, (ref float state, in SpriteComponent value) =>
                {
                    if (value.IsInert)
                    {
                        return true;
                    }

                    value.FrameUpdate(state);
                    return true;
                }, bounds, true);
            }
        }
    }
}
