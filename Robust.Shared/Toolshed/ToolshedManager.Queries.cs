using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;

namespace Robust.Shared.Toolshed;

// This is for information about commands that can be queried, i.e. return type possibilities.

public sealed partial class ToolshedManager
{
    private Dictionary<Type, HashSet<Type>> _typeCache = new();

    internal IEnumerable<Type> AllSteppedTypes(Type t, bool allowVariants = true)
    {
        if (_typeCache.TryGetValue(t, out var cache))
            return cache;
        cache = new(AllSteppedTypesInner(t, allowVariants));
        _typeCache[t] = cache;

        return cache;
    }

    private IEnumerable<Type> AllSteppedTypesInner(Type t, bool allowVariants)
    {
        Type oldT;
        do
        {
            yield return t;
            if (t == typeof(void))
                yield break;

            if (t.IsGenericType && allowVariants)
            {
                foreach (var variant in t.GetVariants(this))
                {
                    yield return variant;
                }
            }

            foreach (var @interface in t.GetInterfaces())
            {
                foreach (var innerT in AllSteppedTypes(@interface, allowVariants))
                {
                    yield return innerT;
                }
            }

            if (t.BaseType is { } baseType)
            {
                foreach (var innerT in AllSteppedTypes(baseType, allowVariants))
                {
                    yield return innerT;
                }
            }

            yield return typeof(IEnumerable<>).MakeGenericType(t);

            oldT = t;
            t = t.StepDownConstraints();
        } while (t != oldT);
    }
}
