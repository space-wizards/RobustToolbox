using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

internal static class ConstraintUtils
{
    public static T RequireActual<T>(object? actual)
    {
        if (actual is T cast)
            return cast;
        throw new ArgumentException($"Expected {typeof(T)} but was {actual?.GetType()}");
    }
    public static EntityUid GetEntityUid(object? actual)
    {
        EntityUid? uid = null;
        if (actual is EntityUid ent)
            uid = ent;
        else
        {
            var field = actual?.GetType().GetField("Owner") ?? actual?.GetType().GetField("Item1");
            if (field is not null)
            {
                uid = (EntityUid?)field.GetValue(actual);
            }
        }

        if (uid is null)
            throw new ArgumentException($"Expected EntityUid or Entity but was {actual?.GetType()}");
        return uid.Value;
    }

    public static IEnumerable<string> HighlightMatches(this IEnumerable<string> items, string match)
    {
        return items.Select(i => i.Equals(match) ? $"***{i}***" : i.ToString());
    }
}
