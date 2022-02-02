using System;
using System.Text;
using Linguini.Bundle.Errors;
using Linguini.Syntax.Parser.Error;

namespace Robust.Shared.Localization;

internal static class LocHelper
{
    public static string FormatCompileErrors(this ParseError self, ReadOnlyMemory<char> resource)
    {
        ErrorSpan span = new(self.Row, self.Slice!.Value.Start.Value, self.Slice.Value.End.Value,
            self.Position.Start.Value, self.Position.End.Value);
        return FormatErrors(self.Message, span, resource);
    }

    private static string FormatErrors(string message, ErrorSpan span, ReadOnlyMemory<char> resource)
    {
        var sb = new StringBuilder();
        var errContext = resource.Slice(span.StartSpan, span.EndSpan - span.StartSpan).ToString();
        sb.AppendLine();
        sb.Append(errContext);

        var currLineOffset = 0;
        for (var i = 0; i < span.StartMark - span.StartSpan; i++)
        {
            switch (errContext[i])
            {
                // Reset current line so that mark aligns with the reported error
                // We cheat here a bit, since we both `\r\n` and `\n` end with '\n'
                case '\n':
                    currLineOffset = 0;
                    break;
                default:
                    currLineOffset++;
                    break;
            }
        }

        sb.AppendLine()
            .Append(' ', currLineOffset)
            .Append('^', span.EndMark - span.StartMark)
            .Append($" {message}");
        return sb.ToString();
    }
}
