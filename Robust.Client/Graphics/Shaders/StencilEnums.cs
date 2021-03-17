namespace Robust.Client.Graphics
{
    public enum StencilFunc : byte
    {
        Always,
        Never,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        NotEqual,
        Equal,
    }

    public enum StencilOp : byte
    {
        Keep,
        Zero,
        Replace,
        IncrementClamp,
        IncrementWrap,
        DecrementClamp,
        DecrementWrap,
        Invert
    }
}
