using System;
using System.IO;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Utility class for abbreviating common type names.
    /// </summary>
    public static class TypeAbbreviation
    {
        private static readonly Abbreviation[] _abbreviations;

        static TypeAbbreviation()
        {
            using var stream =
                typeof(TypeAbbreviation).Assembly.GetManifestResourceStream(
                    "Robust.Shared.Utility.TypeAbbreviations.yaml");
            DebugTools.AssertNotNull(stream);

            using var streamReader = new StreamReader(stream!, EncodingHelpers.UTF8);
            var yamlStream = new YamlStream();

            yamlStream.Load(streamReader);

            var document = yamlStream.Documents[0];

            _abbreviations = ParseAbbreviations((YamlSequenceNode) document.RootNode);
        }

        /// <summary>
        ///     Attempt to produce a shorter version of a type's full representation.
        /// </summary>
        /// <param name="type">The type to abbreviate.</param>
        /// <returns>A shorter representation of the passed type than given by `ToString()`.</returns>
        public static string Abbreviate(Type type)
        {
            if (type.FullName == null)
            {
                return "<unnamed type>";
            }

            var sb = new StringBuilder();

            // `Type.FullName` assembly-qualifies all type arguments, but we don't
            // want them to be qualified - hence, this hack. We just take the name
            // before the type arguments by ignoring characters after the generic
            // argument number marker <c>`</c>.
            AbbreviateName(type.FullName.Split('`')[0], _abbreviations, sb);

            // Never null - this is just empty if the type is non-generic
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0) {
                // Match Type's `ToString()` - start with the number of arguments
                sb.Append("`").Append(genericArgs.Length).Append("[");
                foreach (var genericArg in genericArgs) {
                    AbbreviateName(genericArg.FullName, _abbreviations, sb);
                }
                sb.Append("]");
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Attempt to abbreviate a full name into something shorter.
        ///
        ///     For types, use <see cref="Abbreviate(Type)"/> instead, since it
        ///     correctly handles the more complex type logic.
        /// </summary>
        /// <param name="name">The name to abbreviate.</param>
        /// <returns>A shorter, but still unique, version of the passed named.</returns>
        public static string Abbreviate(string name)
        {
            var sb = new StringBuilder();

            AbbreviateName(name, _abbreviations, sb);

            return sb.ToString();
        }

        private static void AbbreviateName(ReadOnlySpan<char> name, Abbreviation[] abbreviations, StringBuilder output)
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
                    AbbreviateName(name, abbr.SubAbbreviations, output);
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

                if (subNode.TryGetNode("sub", out YamlSequenceNode? node))
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
