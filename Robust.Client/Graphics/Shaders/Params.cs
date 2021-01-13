namespace Robust.Client.Graphics.Shaders
{
    public enum ShaderParamType : byte
    {
        // Can this even happen?
        Void = 0,
        Bool,
        BVec2,
        BVec3,
        BVec4,
        UInt,
        /// <summary>
        ///     While Godot supports all (u)int vectors,
        ///     It doesn't specify which is used from get params.
        ///     So ivec2, ivec3, ivec4... are all this guy.
        /// </summary>
        IntVec,
        Int,
        Float,
        Vec2,
        Vec3,
        Vec4,
        Mat2,
        Mat3,
        Mat4,
        /// <summary>
        ///     Godot supports u, i and b samplers too,
        ///     but we can't tell in code.
        /// </summary>
        Sampler2D,
        SamplerCube
    }
}
