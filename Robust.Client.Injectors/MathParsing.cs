using System.Linq;
using Pidgin;
using static Pidgin.Parser;


namespace Robust.Build.Tasks
{
    public static class MathParsing
    {
        public static Parser<char, float> Single { get; } = Real.Select(c => (float) c);

        public static Parser<char, float> Single1 { get; }
            = Single.Between(SkipWhitespaces);

        public static Parser<char, (float, float)> Single2 { get; }
            = Single.Before(SkipWhitespaces).Repeat(2).Select(e =>
            {
                var arr = e.ToArray();
                return (arr[0], arr[1]);
            });

        public static Parser<char, (float, float, float, float)> Single4 { get; }
            = Single.Before(SkipWhitespaces).Repeat(4).Select(e =>
            {
                var arr = e.ToArray();
                return (arr[0], arr[1], arr[2], arr[3]);
            });

        public static Parser<char, float[]> Thickness { get; }
            = SkipWhitespaces.Then(
                OneOf(
                    Try(Single4.Select(c => new[] {c.Item1, c.Item2, c.Item3, c.Item4})),
                    Try(Single2.Select(c => new[] {c.Item1, c.Item2})),
                    Try(Single1.Select(c => new[] {c}))
                ));
    }
}
