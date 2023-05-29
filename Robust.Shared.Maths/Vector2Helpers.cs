using System.Numerics;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Maths;

public static class Vector2Helpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Normalized(this Vector2 vec)
    {
        var length = vec.Length();
        return new Vector2(vec.X / length, vec.Y / length);
    }

    /// <summary>
    /// Normalizes this vector if its length > 0, otherwise sets it to 0.
    /// </summary>
    public static float Normalize(this Vector2 vec)
    {
        var length = vec.Length();

        if (length < float.Epsilon)
        {
            return 0f;
        }

        var invLength = 1f / length;
        vec.X *= invLength;
        vec.Y *= invLength;
        return length;
    }
}
