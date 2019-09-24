using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.GameObjects
{
    [PublicAPI]
    public static class ComponentExt
    {
        /// <summary>
        ///     Convenience wrapper to implement "create component if it does not already exist".
        ///     Always gives you back a component, and creates it if it does not exist yet.
        /// </summary>
        /// <param name="entity">The entity to fetch or create the component on.</param>
        /// <param name="component">The existing component, or the new component if none existed yet.</param>
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <returns>True if the component already existed, false if it had to be created.</returns>
        public static bool EnsureComponent<T>(this IEntity entity, out T component) where T : Component, new()
        {
            if (entity.TryGetComponent(out component))
            {
                return true;
            }

            component = entity.AddComponent<T>();
            return false;
        }

        /// <summary>
        ///     Convenience wrapper to implement "create component if it does not already exist".
        ///     Always gives you back a component, and creates it if it does not exist yet.
        /// </summary>
        /// <param name="entity">The entity to fetch or create the component on.</param>
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <returns>The existing component, or the new component if none existed yet.</returns>
        public static T EnsureComponent<T>(this IEntity entity) where T : Component, new()
        {
            if (entity.TryGetComponent(out T component))
            {
                return component;
            }

            return entity.AddComponent<T>();
        }
    }
}
