using System;
using System.Text;
using Linguini.Bundle.Errors;
using Linguini.Syntax.Parser.Error;

namespace Robust.Shared.Localization
{
    public static class LocHelper
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
            var row = $"  {span.Row}  ";
            var errContext = resource.Slice(span.StartSpan, span.EndSpan - span.StartSpan).ToString();
            sb.Append(row).Append('|')
                .AppendLine(errContext);
            sb.Append(' ', row.Length).Append('|')
                .Append(' ', span.StartMark - span.StartSpan - 1).Append('^', span.EndMark - span.StartMark)
                .AppendLine($" {message}");
            return sb.ToString();
        }
    }
}