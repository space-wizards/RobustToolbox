using System;
using System.Numerics;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector3TypeParser : SpanLikeTypeParser<Vector3, float>
{
    public override int Elements => 3;
    public override Vector3 Create(Span<float> elements)
    {
        return new Vector3(elements[0], elements[1], elements[2]);
    }
}
