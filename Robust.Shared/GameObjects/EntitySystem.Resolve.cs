using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Resolve<TComp>(EntityUid uid, [NotNullWhen(true)] ref TComp? component)
            where TComp : IComponent
        {
            return component != null || ComponentManager.TryGetComponent(uid, out component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Resolve<TComp1, TComp2>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            return Resolve(uid, ref comp1) && Resolve(uid, ref comp2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Resolve<TComp1, TComp2, TComp3>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, [NotNullWhen(true)] ref TComp3? comp3)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            return Resolve(uid, ref comp1, ref comp2) && Resolve(uid, ref comp3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Resolve<TComp1, TComp2, TComp3, TComp4>(EntityUid uid, [NotNullWhen(true)] ref TComp1? comp1, [NotNullWhen(true)] ref TComp2? comp2, [NotNullWhen(true)] ref TComp3? comp3, [NotNullWhen(true)] ref TComp4? comp4)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            return Resolve(uid, ref comp1, ref comp2) && Resolve(uid, ref comp3, ref comp4);
        }
    }
}
