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
        [Dependency] private readonly RenderingTreeSystem _treeSystem = default!;

        private readonly Queue<SpriteComponent> _inertUpdateQueue = new();
        private HashSet<ISpriteComponent> _manualUpdate = new();

        public override void Initialize()
        {
            base.Initialize();

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

            foreach (var sprite in _manualUpdate)
            {
                if (!sprite.Deleted && !sprite.IsInert)
                    sprite.FrameUpdate(frameTime);
            }

            var pvsBounds = _eyeManager.GetWorldViewbounds();

            var currentMap = _eyeManager.CurrentMap;
            if (currentMap == MapId.Nullspace)
            {
                return;
            }

            foreach (var comp in _treeSystem.GetRenderTrees(currentMap, pvsBounds))
            {
                var bounds = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(comp.Owner.Uid).InvWorldMatrix.TransformBox(pvsBounds);

                comp.SpriteTree.QueryAabb(ref frameTime, (ref float state, in SpriteComponent value) =>
                {
                    if (value.IsInert)
                    {
                        return true;
                    }

                    if (!_manualUpdate.Contains(value))
                        value.FrameUpdate(state);
                    return true;
                }, bounds, true);
            }

            _manualUpdate.Clear();
        }

        /// <summary>
        ///     Force update of the sprite component next frame
        /// </summary>
        public void ForceUpdate(ISpriteComponent sprite)
        {
            _manualUpdate.Add(sprite);
        }
    }
}
