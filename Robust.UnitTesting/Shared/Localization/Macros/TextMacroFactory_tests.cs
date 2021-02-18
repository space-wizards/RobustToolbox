using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Localization.Macros;

namespace Robust.UnitTesting.Shared.Localization.Macros
{
    [TestFixture, Parallelizable, TestOf(typeof(TextMacroFactory))]
    internal class TextMacroFactory_tests : RobustUnitTest
    {
        private TextMacroFactory sut = default!;

        [SetUp]
        public void SetUp()
        {
            sut = new TextMacroFactory();
            IoCManager.InjectDependencies(sut);
        }

        [RegisterTextMacro("mock_macro", "test-TE")]
        private class MockTextMacro : ITextMacro
        {
            public string Format(object? argument)
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void TestResolveLanguageMacro()
        {
            sut.Register("my_macro", typeof(MockTextMacro));
            sut.Register("my_macro_en", "en", typeof(MockTextMacro));
            sut.Register("my_macro_us", "en-US", typeof(MockTextMacro));
            sut.Register("my_macro_gb", "en-GB", typeof(MockTextMacro));
            sut.Register("my_macro_t", "ent", typeof(MockTextMacro));

            var macros = sut.GetMacrosForLanguage("en-US");

            Assert.IsTrue(macros.ContainsKey("my_macro"));
            Assert.IsTrue(macros.ContainsKey("my_macro_en"));
            Assert.IsTrue(macros.ContainsKey("my_macro_us"));
            Assert.IsFalse(macros.ContainsKey("my_macro_gb"));
            Assert.IsFalse(macros.ContainsKey("my_macro_t"));
        }

        [Test]
        public void TestAutoRegistrations()
        {
            sut.DoAutoRegistrations();

            var macros = sut.GetMacrosForLanguage("test-TE");

            Assert.IsTrue(macros.ContainsKey("mock_macro"));
            Assert.IsInstanceOf<MockTextMacro>(macros["mock_macro"]);
        }
    }
}
