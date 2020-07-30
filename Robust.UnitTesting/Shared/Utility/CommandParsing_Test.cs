using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(CommandParsing))]
    public class CommandParsing_Test
    {
        [TestCase("foo bar", new[] {"foo", "bar"})]
        [TestCase("foo  bar", new[] {"foo", "bar"})]
        [TestCase("foo  \"bar baz\"", new[] {"foo", "bar baz"})]
        [TestCase("foo  \"bar  baz\"", new[] {"foo", "bar  baz"})]
        [TestCase("foo  bar  baz", new[] {"foo", "bar", "baz"})]
        [TestCase(@"foo  \""bar\""", new[] {"foo", "\"bar\""})]
        [TestCase(@"foo  \""b\ar\""", new[] {"foo", "\"bar\""})]
        [TestCase("", new string[0])]
        public void TestParse(string command, string[] expected)
        {
            var list = new List<string>();
            CommandParsing.ParseArguments(command, list);

            Assert.That(list, Is.EqualTo(expected));
        }

        [TestCase("foo", "foo")]
        [TestCase(@"f\oo", @"f\\oo")]
        [TestCase(@"f""oo", @"f\""oo")]
        public void TestEscape(string source, string expected)
        {
            var escaped = CommandParsing.Escape(source);

            Assert.That(escaped, Is.EqualTo(expected));
        }
    }
}
