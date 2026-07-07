using System;
using System.Numerics;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector4TypeParser : SpanLikeTypeParser<Vector4, float>
{
    public override int Elements => 4;
    public override Vector4 Create(Span<float> elements)
    {
        return new Vector4(elements[0], elements[1], elements[2], elements[4]);
    }
}
