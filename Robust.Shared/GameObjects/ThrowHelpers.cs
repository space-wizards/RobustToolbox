using System;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects;

internal static class ThrowHelpers
{
    [DoesNotReturn]
    public static void ThrowECSVersionMismatch()
    {
        throw new InvalidOperationException("ECS was modified (component add/rem, entity spawn/del) while enumerating.");
    }
}
