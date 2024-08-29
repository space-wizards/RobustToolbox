using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Linguini.Bundle;
using Linguini.Bundle.Errors;
using Linguini.Syntax.Ast;
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

    public static bool InsertResourcesAndReport(this FluentBundle bundle, Resource resource,
        ResPath path, [NotNullWhen(false)] out List<LocError>? errors)
    {
        if (!bundle.AddResource(resource, out var parseErrors))
        {
            errors = new List<LocError>();
            foreach (var fluentError in parseErrors)
            {
                errors.Add(new LocError(path, fluentError));
            }

            return false;
        }

        errors = null;
        return true;
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
/// <summary>
///  Wrapper around Fluent Error, that adds path to the list of values.
///  Work in progress, FluentErrors need to be modified to be more accessible.
/// </summary>
internal record LocError
{
    public readonly ResPath Path;
    public readonly FluentError Error;

    /// <summary>
    /// Basic constructor.
    /// </summary>
    /// <param name="path">path of resource being added.</param>
    /// <param name="fluentError">FluentError encountered.</param>
    public LocError(ResPath path, FluentError fluentError)
    {
        Path = path;
        Error = fluentError;
    }

    public override string ToString()
    {
        return $"[{Path.CanonPath}]: {Error}";
    }
}
