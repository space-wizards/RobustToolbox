﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;

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

        /// <summary>
        /// Get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of entity system to find.</typeparam>
        /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type.</returns>
        T GetEntitySystem<T>() where T : IEntitySystem;

        /// <summary>
        /// Get an entity system of the specified type, or null if it is not registered.
        /// </summary>
        /// <typeparam name="T">The type of entity system to find.</typeparam>
        /// <returns>The <see cref="IEntitySystem"/> instance matching the specified type, or null.</returns>
        T? GetEntitySystemOrNull<T>() where T : IEntitySystem;

        /// <summary>
        /// Resolves an entity system.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the provided type is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the dependency collection object graph still needs to be constructed for it.
        /// </exception>
        void Resolve<T>([NotNull] ref T? instance)
            where T : IEntitySystem;

        /// <inheritdoc cref="Resolve{T}(ref T?)"/>
        /// <summary>
        /// Resolve two entity systems.
        /// </summary>
        void Resolve<T1, T2>([NotNull] ref T1? instance1, [NotNull] ref T2? instance2)
            where T1 : IEntitySystem
            where T2 : IEntitySystem;

        /// <inheritdoc cref="Resolve{T1, T2}(ref T1?, ref T2?)"/>
        /// <summary>
        /// Resolve three entity systems.
        /// </summary>
        void Resolve<T1, T2, T3>([NotNull] ref T1? instance1, [NotNull] ref T2? instance2, [NotNull] ref T3? instance3)
            where T1 : IEntitySystem
            where T2 : IEntitySystem
            where T3 : IEntitySystem;

        /// <inheritdoc cref="Resolve{T1, T2, T3}(ref T1?, ref T2?, ref T3?)"/>
        /// <summary>
        /// Resolve four entity systems.
        /// </summary>
        void Resolve<T1, T2, T3, T4>([NotNull] ref T1? instance1, [NotNull] ref T2? instance2, [NotNull] ref T3? instance3, [NotNull] ref T4? instance4)
            where T1 : IEntitySystem
            where T2 : IEntitySystem
            where T3 : IEntitySystem
            where T4 : IEntitySystem;

        /// <summary>
        /// Tries to get an entity system of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of entity system to find.</typeparam>
        /// <param name="entitySystem">instance matching the specified type (if exists).</param>
        /// <returns>If an instance of the specified entity system type exists.</returns>
        bool TryGetEntitySystem<T>([NotNullWhen(true)] out T? entitySystem) where T : IEntitySystem;

        /// <summary>
        /// Initialize, discover systems and initialize them through <see cref="IEntitySystem.Initialize"/>.
        /// </summary>
        /// <param name="discover">Whether we should automatically find systems or have they been supplied already.</param>
        /// <seealso cref="IEntitySystem.Initialize"/>
        void Initialize(bool discover = true);

        /// <summary>
        /// Clean up, shut down all systems through <see cref="IEntitySystem.Shutdown"/> and remove them.
        /// </summary>
        /// <seealso cref="IEntitySystem.Shutdown"/>
        void Shutdown();

        void Clear();

        /// <summary>
        /// Update all systems.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        /// <param name="noPredictions">
        /// Only run systems with <see cref="EntitySystem.UpdatesOutsidePrediction"/> set true.
        /// </param>
        /// <seealso cref="IEntitySystem.Update(float)"/>
        void TickUpdate(float frameTime, bool noPredictions);
        void FrameUpdate(float frameTime);

        /// <summary>
        ///     Adds an extra entity system type that otherwise would not be loaded automatically, useful for testing.
        /// </summary>
        /// <typeparam name="T">The type of the entity system to load.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the manager has been initialized already.
        /// </exception>
        void LoadExtraSystemType<T>() where T : IEntitySystem, new();

        IEnumerable<Type> GetEntitySystemTypes();
        bool TryGetEntitySystem(Type sysType, [NotNullWhen(true)] out object? system);
        object GetEntitySystem(Type sysType);

        /// <summary>
        /// Dependency collection that contains all the loaded systems.
        /// </summary>
        public IDependencyCollection DependencyCollection { get; }
    }
}
