using JetBrains.Annotations;
using Robust.Shared.Log;

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
            if (entity.TryGetComponent(out component!))
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
            if (entity.TryGetComponent(out T? component))
            {
                return component;
            }

            return entity.AddComponent<T>();
        }

        /// <summary>
        ///     Convenience wrapper to implement "create component if it does not already exist".
        ///     Always gives you back a component, and creates it if it does not exist yet.
        /// </summary>
        /// <param name="entity">The entity to fetch or create the component on.</param>
        /// <param name="component">The existing component, or the new component if none existed yet.</param>
        /// <param name="warning">
        ///     The custom warning message to log if the component did not exist already.
        ///     Defaults to a predetermined warning if null.
        /// </param>
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <returns>True if the component already existed, false if it had to be created.</returns>
        public static bool EnsureComponentWarn<T>(this IEntity entity, out T component, string? warning = null) where T : Component, new()
        {
            if (entity.TryGetComponent(out component!))
            {
                return true;
            }

            warning ??= $"Entity {entity} at {entity.Transform.MapPosition} did not have a {typeof(T)}";

            Logger.Warning(warning);

            component = entity.AddComponent<T>();
            return false;
        }

        /// <summary>
        ///     Convenience wrapper to implement "create component if it does not already exist".
        ///     Always gives you back a component, and creates it if it does not exist yet.
        /// </summary>
        /// <param name="entity">The entity to fetch or create the component on.</param>
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <param name="warning">
        ///     The custom warning message to log if the component did not exist already.
        ///     Defaults to a predetermined warning if null.
        /// </param>
        /// <returns>The existing component, or the new component if none existed yet.</returns>
        public static T EnsureComponentWarn<T>(this IEntity entity, string? warning = null) where T : Component, new()
        {
            if (entity.TryGetComponent(out T? component))
            {
                return component;
            }

            warning ??= $"Entity {entity} at {entity.Transform.MapPosition} did not have a {typeof(T)}";

            Logger.Warning(warning);

            return entity.AddComponent<T>();
        }
    }
}
