using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Localization.Macros.English;

namespace Robust.UnitTesting.Shared.Localization.Macros
{
    [TestFixture, Parallelizable, TestOf(typeof(MacroFormatProvider))]
    internal class MacroFormatProvider_tests
    {
        private MacroFormatProvider sut = default!;

        private class GenderedPerson : IGenderable
        {
            public string Name;

            public Gender Gender { get; set; }

            public GenderedPerson(string name, Gender gender)
            {
                Name = name;
                Gender = gender;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private readonly GenderedPerson female = new("Lisa", Gender.Female);
        private readonly GenderedPerson male = new("Bob", Gender.Male);
        private readonly GenderedPerson epicene = new("Michel", Gender.Epicene);
        private readonly GenderedPerson neuter = new("D.O.O.R.K.N.O.B.", Gender.Neuter);

        [SetUp]
        public void SetUp()
        {
            var macros = new Dictionary<string, ITextMacro>
            {
                { "they", new They() },
                { "their", new Their() },
                { "theirs", new Theirs() },
                { "them", new Them() },
                { "themself", new Themself() },
                { "theyre", new Theyre() }
            };
            sut = new MacroFormatProvider(new MacroFormatter(macros), CultureInfo.CurrentCulture);
        }

        [Test]
        public void CanFormatNormally()
        {
            AssertFormatNormally("Hello {0}", "world");
            AssertFormatNormally("PI is roughly {0}", 3.1415);
            AssertFormatNormally("Scientific notation: {0:#.##E+0}", 1234.5678);
        }

        private void AssertFormatNormally(string format, params object[] args)
        {
            Assert.That(string.Format(sut, format, args), Is.EqualTo(string.Format(format, args)));
        }

        [Test]
        public void TestInsertThey()
        {
            Assert.That(string.Format(sut, "{0:They} protects", female), Is.EqualTo("She protects"));
            Assert.That(string.Format(sut, "{0:They} attacks", male), Is.EqualTo("He attacks"));
            Assert.That(string.Format(sut, "{0:They} plasmaflood", neuter), Is.EqualTo("It plasmaflood"));
            Assert.That(string.Format(sut, "But most importantly, {0:they} do grammar right", epicene), Is.EqualTo("But most importantly, they do grammar right"));
        }

        [Test]
        public void TestInsertTheir()
        {
            Assert.That(string.Format(sut, "{0:Their} toolbox", female), Is.EqualTo("Her toolbox"));
            Assert.That(string.Format(sut, "{0:Their} toolbox", male), Is.EqualTo("His toolbox"));
            Assert.That(string.Format(sut, "{0:Their} toolbox", neuter), Is.EqualTo("Its toolbox"));
            Assert.That(string.Format(sut, "Grab {0:their} toolbox", epicene), Is.EqualTo("Grab their toolbox"));
        }

        [Test]
        public void TestInsertTheirs()
        {
            Assert.That(string.Format(sut, "{0:Theirs} toolboxs", female), Is.EqualTo("Hers toolboxs"));
            Assert.That(string.Format(sut, "{0:Theirs} toolboxs", male), Is.EqualTo("His toolboxs"));
            Assert.That(string.Format(sut, "{0:Theirs} toolboxs", neuter), Is.EqualTo("Its toolboxs"));
            Assert.That(string.Format(sut, "Grab {0:theirs} toolboxs", epicene), Is.EqualTo("Grab theirs toolboxs"));
        }

        [Test]
        public void TestInsertThem()
        {
            Assert.That(string.Format(sut, "Robust {0:them}", female), Is.EqualTo("Robust her"));
            Assert.That(string.Format(sut, "Robust {0:them}", male), Is.EqualTo("Robust him"));
            Assert.That(string.Format(sut, "Robust {0:them}", neuter), Is.EqualTo("Robust it"));
            Assert.That(string.Format(sut, "Robust {0:them}", epicene), Is.EqualTo("Robust them"));
        }

        [Test]
        public void TestInsertThemself()
        {
            Assert.That(string.Format(sut, "Robust {0:themself}", female), Is.EqualTo("Robust herself"));
            Assert.That(string.Format(sut, "Robust {0:themself}", male), Is.EqualTo("Robust himself"));
            Assert.That(string.Format(sut, "Robust {0:themself}", neuter), Is.EqualTo("Robust itself"));
            Assert.That(string.Format(sut, "Robust {0:themself}", epicene), Is.EqualTo("Robust themself"));
        }

        [Test]
        public void TestInsertTheyre()
        {
            Assert.That(string.Format(sut, "{0:Theyre} robust", female), Is.EqualTo("She's robust"));
            Assert.That(string.Format(sut, "{0:Theyre} robust", male), Is.EqualTo("He's robust"));
            Assert.That(string.Format(sut, "{0:Theyre} robust", neuter), Is.EqualTo("It's robust"));
            Assert.That(string.Format(sut, "{0:Theyre} robust", epicene), Is.EqualTo("They're robust"));
        }

        [Test]
        public void TestUseToString()
        {
            Assert.That(string.Format(sut, "{0} uses {0:their} toolbox", male), Is.EqualTo("Bob uses his toolbox"));
        }
    }
}
