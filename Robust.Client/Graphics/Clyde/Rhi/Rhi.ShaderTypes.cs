using System.Numerics;

namespace Robust.Client.Graphics.Clyde.Rhi;

/// <summary>
/// Equivalent to a WGSL <c>mat3x2f</c>.
/// </summary>
/// <remarks>
/// This matrix is columnar and 3 columns, 2 rows. This is different from the rest of the engine and .NET.
/// </remarks>
public struct ShaderMat3x2F
{
    public float M11;
    public float M12;
    public float M21;
    public float M22;
    public float M31;
    public float M32;

    public static ShaderMat3x2F Transpose(in Matrix3x2 matrix)
    {
        var ret = default(ShaderMat3x2F);
        ret.M11 = matrix.M11;
        ret.M12 = matrix.M12;
        ret.M21 = matrix.M21;
        ret.M22 = matrix.M22;
        ret.M31 = matrix.M31;
        ret.M32 = matrix.M32;
        return ret;
    }
}
