using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Robust.Client.Graphics.Clyde;

/// <summary>
/// Simple C-style preprocessor for WGSL.
/// </summary>
public static partial class ShaderParser2
{
    private static readonly Regex RegexInclude = MyRegex();

    public static string Magic(string source, Func<string, string> resolveInclude)
    {
        var writer = new StringWriter();

        Process(writer, source, resolveInclude);

        return writer.ToString();
    }

    private static void Process(StringWriter writer, string source, Func<string, string> resolveInclude)
    {
        var reader = new StringReader(source);

        while (reader.ReadLine() is { } line)
        {
            var match = RegexInclude.Match(line);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var included = resolveInclude(name);

                Process(writer, included, resolveInclude);
                continue;
            }

            writer.WriteLine(line);
        }
    }

    [GeneratedRegex("^\\s*#\\s*include\\s*<\\s*([^>]+)\\s*>")]
    private static partial Regex MyRegex();
}
