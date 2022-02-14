using System;
using System.Collections.Generic;
using System.Text;
using Linguini.Bundle.Errors;
using Linguini.Syntax.Parser.Error;

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
        var sb = new StringBuilder();
        var errContext = resource.Slice(span.StartSpan, span.EndSpan - span.StartSpan).ToString();
        var lines = new List<ReadOnlyMemory<char>>(5);
        var currLineOffset = 0;
        var lastStart = 0;
        for (var i = 0; i < span.StartMark - span.StartSpan; i++)
        {
            switch (errContext[i])
            {
                // Reset current line so that mark aligns with the reported error
                // We cheat here a bit, since we both `\r\n` and `\n` end with '\n'
                case '\n':
                    if (i > 0 && errContext[i - 1] == '\r')
                    {
                        lines.Add(resource.Slice(lastStart, currLineOffset - 1));
                    }
                    else
                    {
                        lines.Add(resource.Slice(lastStart, currLineOffset));
                    }

                    lastStart = currLineOffset + 1;
                    currLineOffset = 0;
                    break;
                default:
                    currLineOffset++;
                    break;
            }
        }

        lines.Add(resource.Slice(lastStart, resource.Length - lastStart));


        var lastLine = $"{span.Row + lines.Count - 1}".Length;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            sb.Append(newLine ?? Environment.NewLine).Append(' ').Append($"{span.Row + index}".PadLeft(lastLine))
                .Append(" |").Append(line);
        }

        sb.Append(newLine ?? Environment.NewLine)
            .Append(' ', currLineOffset + lastLine + 3)
            .Append('^', span.EndMark - span.StartMark)
            .Append($" {message}");
        return sb.ToString();
    }
}
