using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
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
        public static bool EnsureComponent<T>(this EntityUid entity, out T component) where T : Component, new()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            ref T? comp = ref component!;
            if (entMan.TryGetComponent(entity, out comp))
            {
                return true;
            }

            component = entMan.AddComponent<T>(entity);
            return false;
        }

        /// <summary>
        ///     Convenience wrapper to implement "create component if it does not already exist".
        ///     Always gives you back a component, and creates it if it does not exist yet.
        /// </summary>
        /// <param name="entity">The entity to fetch or create the component on.</param>
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <returns>The existing component, or the new component if none existed yet.</returns>
        public static T EnsureComponent<T>(this EntityUid entity) where T : Component, new()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (entMan.TryGetComponent(entity, out T? component))
            {
                return component;
            }

            return entMan.AddComponent<T>(entity);
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
        public static bool EnsureComponentWarn<T>(this EntityUid entity, out T component, string? warning = null) where T : Component, new()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            ref T? comp = ref component!;
            if (entMan.TryGetComponent(entity, out comp))
            {
                return true;
            }

            warning ??= $"Entity {entity} at {entMan.GetComponent<TransformComponent>(entity).MapPosition} did not have a {typeof(T)}";

            Logger.Warning(warning);

            component = entMan.AddComponent<T>(entity);
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
        public static T EnsureComponentWarn<T>(this EntityUid entity, string? warning = null) where T : Component, new()
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (entMan.TryGetComponent(entity, out T? component))
            {
                return component;
            }

            warning ??= $"Entity {entity} at {entMan.GetComponent<TransformComponent>(entity).MapPosition} did not have a {typeof(T)}";

            Logger.Warning(warning);

            return entMan.AddComponent<T>(entity);
        }

        public static IComponent SetAndDirtyIfChanged<TValue>(
            this IComponent comp,
            ref TValue backingField,
            TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(backingField, value))
            {
                return comp;
            }

            backingField = value;
            comp.Dirty();

            return comp;
        }
    }
}
