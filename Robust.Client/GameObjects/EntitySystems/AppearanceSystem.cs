using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    internal sealed class AppearanceSystem : EntitySystem
    {
        private readonly Queue<ClientAppearanceComponent> _queuedUpdates = new();

        public void EnqueueUpdate(ClientAppearanceComponent component)
        {
            _queuedUpdates.Enqueue(component);
        }

        public override void FrameUpdate(float frameTime)
        {
            while (_queuedUpdates.TryDequeue(out var appearance))
            {
                if (appearance.Deleted)
                    continue;

                OnChangeData(appearance.Owner, appearance);
                appearance.UnmarkDirty();
            }
        }

        public void OnChangeData(EntityUid uid, ClientAppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref appearanceComponent, false)) return;

            foreach (var visualizer in appearanceComponent.Visualizers)
            {
                visualizer.OnChangeData(appearanceComponent);
            }
        }
    }
}
