using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        /// <summary>
        ///     Resolves the component on the entity but only if the component instance is null.
        /// </summary>
        /// <param name="uid">The entity where to query the components.</param>
        /// <param name="component">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="logMissing">Whether to log missing components.</param>
        /// <typeparam name="TComp">The component type to resolve.</typeparam>
        /// <returns>True if the component is not null or was resolved correctly, false if the component couldn't be resolved.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Resolve<TComp>(EntityUid uid, [NotNullWhen(true)] ref TComp? component, bool logMissing = true)
            where TComp : IComponent
        {
            DebugTools.Assert(component == null || uid == component.Owner, "Specified Entity is not the component's Owner!");

            if (component != null)
                return true;

            var found = EntityManager.TryGetComponent(uid, out component);

            if(logMissing && !found)
                Logger.ErrorS("resolve", $"Can't resolve \"{typeof(TComp)}\" on entity {uid}!\n{new StackTrace(1, true)}");

            return found;
        }

        /// <summary>
        ///     Resolves the components on the entity for the null component references.
        /// </summary>
        /// <param name="uid">The entity where to query the components.</param>
        /// <param name="comp1">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp2">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="logMissing">Whether to log missing components.</param>
        /// <typeparam name="TComp1">The component type to resolve.</typeparam>
        /// <typeparam name="TComp2">The component type to resolve.</typeparam>
        /// <returns>True if the components are not null or were resolved correctly, false if any of the component couldn't be resolved.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Resolve<TComp1, TComp2>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, bool logMissing = true)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            return Resolve(uid, ref comp1, logMissing) & Resolve(uid, ref comp2, logMissing);
        }

        /// <summary>
        ///     Resolves the components on the entity for the null component references.
        /// </summary>
        /// <param name="uid">The entity where to query the components.</param>
        /// <param name="comp1">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp2">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp3">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="logMissing">Whether to log missing components.</param>
        /// <typeparam name="TComp1">The component type to resolve.</typeparam>
        /// <typeparam name="TComp2">The component type to resolve.</typeparam>
        /// <typeparam name="TComp3">The component type to resolve.</typeparam>
        /// <returns>True if the components are not null or were resolved correctly, false if any of the component couldn't be resolved.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Resolve<TComp1, TComp2, TComp3>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, [NotNullWhen(true)] ref TComp3? comp3, bool logMissing = true)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            return Resolve(uid, ref comp1, ref comp2, logMissing) & Resolve(uid, ref comp3, logMissing);
        }

        /// <summary>
        ///     Resolves the components on the entity for the null component references.
        /// </summary>
        /// <param name="uid">The entity where to query the components.</param>
        /// <param name="comp1">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp2">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp3">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="comp4">A reference to the variable storing the component, or null if it has to be resolved.</param>
        /// <param name="logMissing">Whether to log missing components.</param>
        /// <typeparam name="TComp1">The component type to resolve.</typeparam>
        /// <typeparam name="TComp2">The component type to resolve.</typeparam>
        /// <typeparam name="TComp3">The component type to resolve.</typeparam>
        /// <typeparam name="TComp4">The component type to resolve.</typeparam>
        /// <returns>True if the components are not null or were resolved correctly, false if any of the component couldn't be resolved.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Resolve<TComp1, TComp2, TComp3, TComp4>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, [NotNullWhen(true)] ref TComp3? comp3, [NotNullWhen(true)] ref TComp4? comp4, bool logMissing = true)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            return Resolve(uid, ref comp1, ref comp2, logMissing) & Resolve(uid, ref comp3, ref comp4, logMissing);
        }
    }
}
