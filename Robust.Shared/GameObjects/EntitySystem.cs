using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     A subsystem that acts on all components of a type at once.
    /// </summary>
    /// <remarks>
    ///     This class is instantiated by the <c>EntitySystemManager</c>, and any IoC Dependencies will be resolved.
    /// </remarks>
    [Reflect(false), PublicAPI]
    public abstract partial class EntitySystem : EntManProxy, IEntitySystem, IPostInjectInit
    {
        [Dependency] protected readonly ILogManager LogManager = default!;
        [Dependency] protected readonly ILocalizationManager Loc = default!;

        public ISawmill Log { get; private set; } = default!;

        protected virtual string SawmillName
        {
            get
            {
                var name = GetType().Name;

                // Strip trailing "system"
                if (name.EndsWith("System"))
                    name = name.Substring(0, name.Length - "System".Length);

                // Convert CamelCase to snake_case
                // Ignore if all uppercase, assume acronym (e.g. NPC or HTN)
                if (name.All(char.IsUpper))
                {
                    name = name.ToLower(CultureInfo.InvariantCulture);
                }
                else
                {
                    name = string.Concat(name.Select(x => char.IsUpper(x) ? $"_{char.ToLower(x)}" : x.ToString()));
                    name = name.Trim('_');
                }

                return $"system.{name}";
            }
        }

        protected internal List<Type> UpdatesAfter { get; } = new();
        protected internal List<Type> UpdatesBefore { get; } = new();

        public bool UpdatesOutsidePrediction { get; protected internal set; }

        IEnumerable<Type> IEntitySystem.UpdatesAfter => UpdatesAfter;
        IEnumerable<Type> IEntitySystem.UpdatesBefore => UpdatesBefore;

        protected EntitySystem()
        {
            Subs = new Subscriptions(this);
        }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void Initialize() { }

        /// <inheritdoc />
        /// <remarks>
        /// Not ran on the client if prediction is disabled and
        /// <see cref="UpdatesOutsidePrediction"/> is false (the default).
        /// </remarks>
        [MustCallBase(true)]
        public virtual void Update(float frameTime) { }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void FrameUpdate(float frameTime) { }

        /// <inheritdoc />
        [MustCallBase(true)]
        public virtual void Shutdown()
        {
            ShutdownSubscriptions();
        }

        #region Static Helpers
        /*
         NOTE: Static helpers relating to EntitySystems are here rather than in a
         static helper class for conciseness / usability. If we had an "EntitySystems" static class
         it would conflict with any imported namespace called "EntitySystems" and require using alias directive, and
         if we called it something longer like "EntitySystemUtility", writing out "EntitySystemUtility.Get" seems
         pretty tedious for a potentially commonly-used method. Putting it here allows writing "EntitySystem.Get"
         which is nice and concise.
         */

        /// <summary>
        /// Gets the indicated entity system.
        /// </summary>
        /// <typeparam name="T">entity system to get</typeparam>
        /// <returns></returns>
        [Obsolete("Either use a dependency, resolve and cache IEntityManager manually, or use EntityManager.System<T>()")]
        public static T Get<T>() where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<T>();
        }

        /// <summary>
        /// Tries to get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of entity system to find.</typeparam>
        /// <param name="entitySystem">instance matching the specified type (if exists).</param>
        /// <returns>If an instance of the specified entity system type exists.</returns>
        [Obsolete("Either use a dependency, resolve and cache IEntityManager manually, or use EntityManager.System<T>()")]
        public static bool TryGet<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem
        {
            return IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem(out entitySystem);
        }

        #endregion


        void IPostInjectInit.PostInject() => PostInject();

        protected virtual void PostInject()
        {
            Log = LogManager.GetSawmill(SawmillName);

#if !DEBUG
            Log.Level = LogLevel.Info;
#endif
        }
    }
}
