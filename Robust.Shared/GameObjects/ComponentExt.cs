using System;
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
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <returns>The existing component, or the new component if none existed yet.</returns>
        [Obsolete]
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
        /// <typeparam name="T">The type of the component to fetch or create.</typeparam>
        /// <param name="warning">
        ///     The custom warning message to log if the component did not exist already.
        ///     Defaults to a predetermined warning if null.
        /// </param>
        /// <returns>The existing component, or the new component if none existed yet.</returns>
        [Obsolete]
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

        [Obsolete]
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
