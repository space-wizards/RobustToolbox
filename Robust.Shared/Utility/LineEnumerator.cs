using System;

namespace Robust.Shared.Utility;

/// <summary>
/// Helper for iterating over the lines in some text.
/// </summary>
/// <remarks>
/// Supports both LF and CRLF.
/// </remarks>
internal struct LineEnumerator
{
    private readonly ReadOnlyMemory<char> _text;
    private int _curPos;

    /// <param name="text">The memory that will be scanned over for lines.</param>
    public LineEnumerator(ReadOnlyMemory<char> text)
    {
        _text = text;
    }

    /// <summary>
    /// Scan for the next line. Gives back the line's start and end position in the given memory.
    /// </summary>
    /// <remarks>
    /// The returned end position includes the newline character (LF or CRLF),
    /// except for the last line in the data if there's no trailing newline.
    /// </remarks>
    /// <returns>True if another line is available, false if the given text is exhausted.</returns>
    public bool MoveNext(out int start, out int end)
    {
        if (_curPos == _text.Length)
        {
            start = 0;
            end = 0;
            return false;
        }

        var foundIndex = _text.Span[_curPos..].IndexOf('\n');
        int nextPos;
        if (foundIndex == -1)
            nextPos = _text.Length;
        else
            nextPos = foundIndex + _curPos + 1;

        start = _curPos;
        end = nextPos;

        _curPos = nextPos;
        return true;
    }
}
