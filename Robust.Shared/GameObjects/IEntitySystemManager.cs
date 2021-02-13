using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Like SS13's master controller. It controls <see cref="IEntitySystem"/> instances.
    /// These instances have set of rules for which components they can run over.
    /// </summary>
    /// <remarks>
    /// The management of these amounts to a couple of things:
    /// <list type="bullet">
    /// <item>
    /// <description>Periodically ticking them through <see cref="IEntitySystem.Update(float)"/>.</description>
    /// </item>
    /// </list>
    /// Periodically ticks <see cref="IEntitySystem"/> instances.
    /// </remarks>
    /// <seealso cref="IEntitySystem"/>
    public interface IEntitySystemManager
    {
        bool MetricsEnabled { get; set; }

        /// <summary>
        /// A new entity system has been loaded into the manager.
        /// </summary>
        event EventHandler<SystemChangedArgs> SystemLoaded;

        /// <summary>
        /// An existing entity system has been unloaded from the manager.
        /// </summary>
        event EventHandler<SystemChangedArgs> SystemUnloaded;

        IReadOnlyCollection<IEntitySystem> AllSystems { get; }

        /// <summary>
        /// Get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of entity system to find.</typeparam>
        /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type.</returns>
        T GetEntitySystem<T>() where T : IEntitySystem;

        /// <summary>
        /// Tries to get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of entity system to find.</typeparam>
        /// <param name="entitySystem">instance matching the specified type (if exists).</param>
        /// <returns>If an instance of the specified entity system type exists.</returns>
        bool TryGetEntitySystem<T>(out T entitySystem) where T : IEntitySystem;

        /// <summary>
        /// Initialize, discover systems and initialize them through <see cref="IEntitySystem.Initialize"/>.
        /// </summary>
        /// <seealso cref="IEntitySystem.Initialize"/>
        void Initialize();

        /// <summary>
        /// Clean up, shut down all systems through <see cref="IEntitySystem.Shutdown"/> and remove them.
        /// </summary>
        /// <seealso cref="IEntitySystem.Shutdown"/>
        void Shutdown();

        /// <summary>
        /// Update all systems.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        /// <seealso cref="IEntitySystem.Update(float)"/>
        void Update(float frameTime);
        void FrameUpdate(float frameTime);

        /// <summary>
        ///     Adds an extra entity system type that otherwise would not be loaded automatically, useful for testing.
        /// </summary>
        /// <typeparam name="T">The type of the entity system to load.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the manager has been initialized already.
        /// </exception>
        void LoadExtraSystemType<T>() where T : IEntitySystem, new();
    }
}
