using System;
using System.Text;
using Linguini.Bundle.Errors;
using Linguini.Syntax.Parser.Error;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization;

internal static class LocHelper
{
    public static string FormatCompileErrors(this ParseError self, ReadOnlyMemory<char> resource,
        string? newLine = null)
    {
        ErrorSpan span = new(self.Row, self.Slice!.Value.Start.Value, self.Slice.Value.End.Value,
            self.Position.Start.Value, self.Position.End.Value);
        return FormatErrors(self.Message, span, resource, newLine);
    }

    private static string FormatErrors(string message, ErrorSpan span, ReadOnlyMemory<char> resource, string? newLine)
    {
        newLine ??= Environment.NewLine;

        var lines = new ValueList<(int start, int end)>();

        // Isolate the lines in the context
        // Also figure out the line number for the 1st line (1-indexed).
        var startLineNumber = -1;
        var markOffset = 0;
        var curLineNumber = 0;
        var lineEnumerator = new LineEnumerator(resource);
        while (lineEnumerator.MoveNext(out var lineStart, out var lineEnd))
        {
            curLineNumber += 1;
            if (span.StartSpan >= lineEnd)
                continue;

            if (span.EndSpan <= lineStart)
                break;

            lines.Add((lineStart, lineEnd));
            if (startLineNumber == -1)
                startLineNumber = curLineNumber;

            if (span.StartMark < lineEnd && span.StartMark >= lineStart)
                markOffset = span.StartMark - lineStart;
        }

        // Figure out width of line number column.
        var lastLine = lines.Count + startLineNumber - 1;
        var lastLineNumberWidth = $"{lastLine}".Length;

        var sb = new StringBuilder();
        curLineNumber = startLineNumber;
        foreach (var (lineStart, lineEnd) in lines)
        {
            var linePadded = $"{curLineNumber}".PadLeft(lastLineNumberWidth);
            var line = resource.Span[lineStart..lineEnd];
            sb.Append($" {linePadded} |{line.TrimEnd()}");
            sb.Append(newLine);

            curLineNumber += 1;
        }

        sb.Append(' ', markOffset + lastLineNumberWidth + 3);
        sb.Append('^', span.EndMark - span.StartMark);
        sb.Append($" {message}");

        return sb.ToString();
    }
}
