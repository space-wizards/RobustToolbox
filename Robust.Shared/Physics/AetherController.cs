using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Physics
{
    // Sloth TODO: For now I've left FilterData out as we use layers and masks which seem cleaner than categories
    // Also Box2D uses the equivalent of system controllers rather than having a controller on each entity individually.
    public abstract class AetherController
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;

        public bool Enabled = true;
        public PhysicsMap World { get; internal set; } = default!;

        /// <summary>
        ///     Helper to get the components relevant for this controller's map
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected IEnumerable<IComponent> GetComponents(Type type)
        {
            foreach (var comp in _componentManager.GetAllComponents(type, false))
            {
                if (comp.Owner.Transform.MapID != World.MapId) continue;
                yield return comp;
            }
        }

        /// <summary>
        ///     Helper to get the components relevant for this controller's map
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected IEnumerable<(T, U)> GetComponents<T, U>() where T : IComponent where U : IComponent
        {
            foreach (var (comp1, comp2) in _componentManager.EntityQuery<T, U>())
            {
                if (comp1.Owner.Transform.MapID != World.MapId) continue;
                yield return (comp1, comp2);
            }
        }

        /// <summary>
        ///     Helper to get the components relevant for this controller's map
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected IEnumerable<(T, U, V)> GetComponents<T, U, V>() where T : IComponent where U : IComponent where V : IComponent
        {
            foreach (var (comp1, comp2, comp3) in _componentManager.EntityQuery<T, U, V>())
            {
                if (comp1.Owner.Transform.MapID != World.MapId) continue;
                yield return (comp1, comp2, comp3);
            }
        }

        public virtual void Update(float frameTime) {}

        public virtual void FrameUpdate(float frameTime) {}
    }
}
