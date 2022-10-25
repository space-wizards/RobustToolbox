using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

// bresenhams line algorithm
// this is slightly rewritten version of code bellow
// https://stackoverflow.com/a/11683720

/// <summary>
///     Iterate points of grid line from start to finish.
/// </summary>
/// <remarks>
///     Start and finish points are included. The order is guaranteed
///     and always goes from start to finish.
/// </remarks>
public struct GridLineEnumerator
{
    private int _x, _y, _i, _numerator;
    private readonly int _dx1, _dy1, _dx2, _dy2;
    private readonly int _longest, _shortest;

    public GridLineEnumerator(Vector2i start, Vector2i finish)
        : this(start.X, start.Y, finish.X, finish.Y)
    {

    }

    public GridLineEnumerator(int x, int y, int x2, int y2)
    {
        _x = x;
        _y = y;

        var w = x2 - x;
        var h = y2 - y;

        _dx1 = Math.Sign(w);
        _dy1 = Math.Sign(h);
        _dx2 = Math.Sign(w);
        _dy2 = 0;

        _longest = Math.Abs(w);
        _shortest = Math.Abs(h);
        if (_longest <= _shortest)
        {
            (_longest, _shortest) = (_shortest, _longest);
            _dx2 = 0;
            _dy2 = Math.Sign(h);
        }

        _numerator = _longest / 2;
        _i = -1;
    }

    public Vector2i Current => new(_x, _y);

    public bool MoveNext()
    {
        if (_i >= _longest)
            return false;

        _i++;
        if (_i == 0)
            return true;

        _numerator += _shortest;
        if (_numerator >= _longest)
        {
            _numerator -= _longest;
            _x += _dx1;
            _y += _dy1;
        }
        else
        {
            _x += _dx2;
            _y += _dy2;
        }

        return true;
    }
}
