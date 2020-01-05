using System;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Utility
{
    public static class TypeAbbreviation
    {
        private static readonly Abbreviation[] _abbreviations;

        static TypeAbbreviation()
        {
            using var stream =
                typeof(TypeAbbreviation).Assembly.GetManifestResourceStream(
                    "Robust.Shared.Utility.TypeAbbreviations.yaml");
            DebugTools.AssertNotNull(stream);

            using var streamReader = new StreamReader(stream, EncodingHelpers.UTF8);
            var yamlStream = new YamlStream();

            yamlStream.Load(streamReader);

            var document = yamlStream.Documents[0];

            _abbreviations = ParseAbbreviations((YamlSequenceNode) document.RootNode);
        }

        public static string Abbreviate(ReadOnlySpan<char> name)
        {
            var sb = new StringBuilder();

            Abbreviate(name, _abbreviations, sb);

            return sb.ToString();
        }

        private static void Abbreviate(ReadOnlySpan<char> name, Abbreviation[] abbreviations, StringBuilder output)
        {
            foreach (var abbr in abbreviations)
            {
                if (!name.StartsWith(abbr.Long))
                {
                    continue;
                }

                output.Append(abbr.Short);

                name = name[abbr.Long.Length..];

                if (abbr.SubAbbreviations.Length != 0)
                {
                    Abbreviate(name, abbr.SubAbbreviations, output);
                    // Return so nested call can handle appending final name.
                    return;
                }

                // Break to append rest of name.
                break;
            }

            output.Append(name);
        }

        private static Abbreviation[] ParseAbbreviations(YamlSequenceNode sequence)
        {
            var array = new Abbreviation[sequence.Children.Count];

            for (var i = 0; i < array.Length; i++)
            {
                var subNode = (YamlMappingNode)sequence[i];

                // TODO: Maybe allow opting out of those mandatory periods at the end.
                var longName = subNode.GetNode("long").AsString() + ".";
                var shortName = subNode.GetNode("short").AsString() + ".";
                var sub = Array.Empty<Abbreviation>();

                if (subNode.TryGetNode("sub", out YamlSequenceNode node))
                {
                    sub = ParseAbbreviations(node);
                }

                var abbr = new Abbreviation
                {
                    Long = longName,
                    Short = shortName,
                    SubAbbreviations = sub
                };

                array[i] = abbr;
            }

            return array;
        }

        private struct Abbreviation
        {
            public string Long { get; set; }
            public string Short { get; set; }

            public Abbreviation[] SubAbbreviations { get; set; }
        }
    }
}
