using System.Numerics;
using System.Runtime.InteropServices;

namespace Robust.Client.Graphics.Rhi;

/// <summary>
/// Equivalent to a WGSL <c>mat2x3f</c>.
/// </summary>
/// <remarks>
/// This matrix is columnar and 2 columns, 3 rows. This is equivalent to .NET's <see cref="Matrix3x2"/>!
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public struct ShaderMat2x3F
{
    [FieldOffset(0)]
    public float M11;
    [FieldOffset(4)]
    public float M21;
    [FieldOffset(8)]
    public float M31;

    [FieldOffset(16)]
    public float M12;
    [FieldOffset(20)]
    public float M22;
    [FieldOffset(24)]
    public float M32;

    public static ShaderMat2x3F FromMatrix(in Matrix3x2 matrix)
    {
        var ret = default(ShaderMat2x3F);
        ret.M11 = matrix.M11;
        ret.M12 = matrix.M12;
        ret.M21 = matrix.M21;
        ret.M22 = matrix.M22;
        ret.M31 = matrix.M31;
        ret.M32 = matrix.M32;
        return ret;
    }
}
