using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility
{
    public static class EntitySystemHelpers
    {

        /// <summary>
        /// Gets the indicated entity system.
        /// </summary>
        /// <typeparam name="T">entity system to get</typeparam>
        /// <returns></returns>
        public static T EntitySystem<T>() where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<T>();
        }

        /// <summary>
        /// Tries to get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of entity system to find.</typeparam>
        /// <param name="entitySystem">instance matching the specified type (if exists).</param>
        /// <returns>If an instance of the specified entity system type exists.</returns>
        public static bool TryGetEntitySystem<T>(out T entitySystem) where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<T>(out entitySystem);
        }
    }
}
