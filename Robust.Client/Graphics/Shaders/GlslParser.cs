using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Robust.Shared.Maths;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Client.Graphics.Shaders
{
    internal static class GlslParser
    {
        // Don't care about enforcing exact rules in our parser, .NET will do that.
        public static readonly Parser<char, char> NumberChar =
            OneOf(LetterOrDigit, Char('+'), Char('-'), Char('.'));

        public static readonly Parser<char, string> Number = NumberChar.ManyString().Labelled("number");

        public static readonly Parser<char, string> Bool =
            Try(String("true"))
                .Or(String("false"))
                .Labelled("boolean");

        public static Parser<char, IEnumerable<string>> VecBasic(string type, Parser<char, string> element) =>
            String(type)
                .Between(SkipWhitespaces)
                .Then(element
                    .Between(SkipWhitespaces)
                    .Separated(Char(','))
                    .Between(Char('('), Char(')')));

        public static Parser<char, float[]> ParserFloatVec(int count) =>
            VecBasic($"vec{count}", Number)
                .Select(c =>
                {
                    var arr = c.Select(f => float.Parse(f, CultureInfo.InvariantCulture)).ToArray();
                    if (arr.Length != count)
                    {
                        throw new FormatException();
                    }

                    return arr;
                });

        public static readonly Parser<char, Vector2> ParserVec2 =
            ParserFloatVec(2)
                .Select(a => new Vector2(a[0], a[1]))
                .Labelled("vec2");

        public static readonly Parser<char, Vector3> ParserVec3 =
            ParserFloatVec(3)
                .Select(a => new Vector3(a[0], a[1], a[2]))
                .Labelled("vec3");

        public static readonly Parser<char, Vector4> ParserVec4 =
            ParserFloatVec(4)
                .Select(a => new Vector4(a[0], a[1], a[2], a[3]))
                .Labelled("vec4");


        public static Parser<char, int[]> ParserIntVec(int count) =>
            VecBasic($"ivec{count}", Number)
                .Select(c =>
                {
                    var arr = c.Select(f => int.Parse(f, CultureInfo.InvariantCulture)).ToArray();
                    if (arr.Length != count)
                    {
                        throw new FormatException();
                    }

                    return arr;
                });

        public static readonly Parser<char, Vector2i> ParserIVec2 =
            ParserIntVec(2)
                .Select(a => new Vector2i(a[0], a[1]))
                .Labelled("ivec2");

        public static readonly Parser<char, Vector3i> ParserIVec3 =
            ParserIntVec(3)
                .Select(a => new Vector3i(a[0], a[1], a[2]))
                .Labelled("ivec3");

        public static readonly Parser<char, Vector4i> ParserIVec4 =
            ParserIntVec(4)
                .Select(a => new Vector4i(a[0], a[1], a[2], a[3]))
                .Labelled("ivec4");

        public static Parser<char, uint[]> ParserUIntVec(int count) =>
            VecBasic($"uvec{count}", Number)
                .Select(c =>
                {
                    var arr = c.Select(f => uint.Parse(f, CultureInfo.InvariantCulture)).ToArray();
                    if (arr.Length != count)
                    {
                        throw new FormatException();
                    }

                    return arr;
                });

        public static readonly Parser<char, Vector2u> ParserUVec2 =
            ParserUIntVec(2)
                .Select(a => new Vector2u(a[0], a[1]))
                .Labelled("uvec2");

        public static readonly Parser<char, (uint x, uint y, uint z)> ParserUVec3 =
            ParserUIntVec(3)
                .Select(a => (a[0], a[1], a[2]))
                .Labelled("uvec3");

        public static readonly Parser<char, (uint x, uint y, uint z, uint w)> ParserUVec4 =
            ParserUIntVec(4)
                .Select(a => (a[0], a[1], a[2], a[3]))
                .Labelled("uvec4");


        public static Parser<char, bool[]> ParserBoolVec(int count) =>
            VecBasic($"bvec{count}", Number)
                .Select(c =>
                {
                    var arr = c.Select(bool.Parse).ToArray();
                    if (arr.Length != count)
                    {
                        throw new FormatException();
                    }

                    return arr;
                });

        public static readonly Parser<char, (bool, bool)> ParserBVec2 =
            ParserBoolVec(2)
                .Select(a => (a[0], a[1]))
                .Labelled("bvec2");

        public static readonly Parser<char, (bool, bool, bool)> ParserBVec3 =
            ParserBoolVec(3)
                .Select(a => (a[0], a[1], a[2]))
                .Labelled("bvec3");

        public static readonly Parser<char, (bool, bool, bool, bool)> ParserBVec4 =
            ParserBoolVec(4)
                .Select(a => (a[0], a[1], a[2], a[3]))
                .Labelled("bvec4");


        public static Parser<char, double[]> ParserDoubleVec(int count) =>
            VecBasic($"dvec{count}", Number)
                .Select(c =>
                {
                    var arr = c.Select(f => double.Parse(f, CultureInfo.InvariantCulture)).ToArray();
                    if (arr.Length != count)
                    {
                        throw new FormatException();
                    }

                    return arr;
                });

        public static readonly Parser<char, Vector2d> ParserDVec2 =
            ParserDoubleVec(2)
                .Select(a => new Vector2d(a[0], a[1]))
                .Labelled("dvec2");

        public static readonly Parser<char, Vector3d> ParserDVec3 =
            ParserDoubleVec(3)
                .Select(a => new Vector3d(a[0], a[1], a[2]))
                .Labelled("dvec3");

        public static readonly Parser<char, Vector4d> ParserDVec4 =
            ParserDoubleVec(4)
                .Select(a => new Vector4d(a[0], a[1], a[2], a[3]))
                .Labelled("dvec4");
    }
}
