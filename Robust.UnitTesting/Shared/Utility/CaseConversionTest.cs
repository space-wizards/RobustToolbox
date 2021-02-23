using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    [TestOf(typeof(CaseConversion))]
    public sealed class CaseConversionTest
    {
        [Test]
        [TestCase("FooBar", ExpectedResult = "foo-bar")]
        [TestCase("Foo", ExpectedResult = "foo")]
        [TestCase("FooBarBaz", ExpectedResult = "foo-bar-baz")]
        [TestCase("AssistantPDA", ExpectedResult = "assistant-pda")] // incorrect abbreviations
        [TestCase("AssistantPda", ExpectedResult = "assistant-pda")] // correct abbreviations
        [TestCase("FileIO", ExpectedResult = "file-io")]
        public string PascalToKebab(string input)
        {
            return CaseConversion.PascalToKebab(input);
        }
    }
}
