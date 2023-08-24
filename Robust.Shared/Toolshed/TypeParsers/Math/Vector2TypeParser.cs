using System;
using System.Numerics;

namespace Robust.Shared.Toolshed.TypeParsers.Math;

internal sealed class Vector2TypeParser : SpanLikeTypeParser<Vector2, float>
{
    public override int Elements => 2;
    public override Vector2 Create(Span<float> elements)
    {
        return new Vector2(elements);
    }
}