using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Debugging;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Client.Debugging
{
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency]
        readonly IComponentManager componentManager;

        private bool _debugColliders = false;
        public bool DebugColliders
        {
            get => _debugColliders;
            set
            {
                if (value == DebugColliders)
                {
                    return;
                }
                _debugColliders = value;

                UpdateDebugColliders();
            }
        }

        private void UpdateDebugColliders()
        {
            int count = 0;
            foreach (var component in componentManager.GetComponents<GodotCollidableComponent>())
            {
                count++;
                component.DebugDraw = DebugColliders;
            }

            Logger.Debug($"Set debugdraw on {count} collidables");
        }
    }
}
