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
                    return;

                foreach (var visualizer in appearance.Visualizers)
                {
                    visualizer.OnChangeData(appearance);
                }

                appearance.UnmarkDirty();
            }
        }
    }
}
