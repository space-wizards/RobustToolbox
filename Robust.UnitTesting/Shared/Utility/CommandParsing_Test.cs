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
        [TestCase("", new string[0])]
        public void Test(string command, string[] expected)
        {
            var list = new List<string>();
            CommandParsing.ParseArguments(command, list);

            Assert.That(list, Is.EquivalentTo(expected));
        }
    }
}
