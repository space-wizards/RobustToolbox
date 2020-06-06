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

        private readonly GenderedPerson female = new GenderedPerson("Lisa", Gender.Female);
        private readonly GenderedPerson male = new GenderedPerson("Bob", Gender.Male);
        private readonly GenderedPerson epicene = new GenderedPerson("Michel", Gender.Epicene);
        private readonly GenderedPerson neuter = new GenderedPerson("D.O.O.R.K.N.O.B.", Gender.Neuter);

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
            Assert.AreEqual(string.Format(format, args), string.Format(sut, format, args));
        }

        [Test]
        public void TestInsertThey()
        {
            Assert.AreEqual("She protects", string.Format(sut, "{0:They} protects", female));
            Assert.AreEqual("He attacks", string.Format(sut, "{0:They} attacks", male));
            Assert.AreEqual("It plasmaflood", string.Format(sut, "{0:They} plasmaflood", neuter));
            Assert.AreEqual("But most importantly, they do grammar right", string.Format(sut, "But most importantly, {0:they} do grammar right", epicene));
        }

        [Test]
        public void TestInsertTheir()
        {
            Assert.AreEqual("Her toolbox", string.Format(sut, "{0:Their} toolbox", female));
            Assert.AreEqual("His toolbox", string.Format(sut, "{0:Their} toolbox", male));
            Assert.AreEqual("Its toolbox", string.Format(sut, "{0:Their} toolbox", neuter));
            Assert.AreEqual("Grab their toolbox", string.Format(sut, "Grab {0:their} toolbox", epicene));
        }

        [Test]
        public void TestInsertTheirs()
        {
            Assert.AreEqual("Hers toolboxs", string.Format(sut, "{0:Theirs} toolboxs", female));
            Assert.AreEqual("His toolboxs", string.Format(sut, "{0:Theirs} toolboxs", male));
            Assert.AreEqual("Its toolboxs", string.Format(sut, "{0:Theirs} toolboxs", neuter));
            Assert.AreEqual("Grab theirs toolboxs", string.Format(sut, "Grab {0:theirs} toolboxs", epicene));
        }

        [Test]
        public void TestInsertThem()
        {
            Assert.AreEqual("Robust her", string.Format(sut, "Robust {0:them}", female));
            Assert.AreEqual("Robust him", string.Format(sut, "Robust {0:them}", male));
            Assert.AreEqual("Robust it", string.Format(sut, "Robust {0:them}", neuter));
            Assert.AreEqual("Robust them", string.Format(sut, "Robust {0:them}", epicene));
        }

        [Test]
        public void TestInsertThemself()
        {
            Assert.AreEqual("Robust herself", string.Format(sut, "Robust {0:themself}", female));
            Assert.AreEqual("Robust himself", string.Format(sut, "Robust {0:themself}", male));
            Assert.AreEqual("Robust itself", string.Format(sut, "Robust {0:themself}", neuter));
            Assert.AreEqual("Robust themself", string.Format(sut, "Robust {0:themself}", epicene));
        }

        [Test]
        public void TestInsertTheyre()
        {
            Assert.AreEqual("She's robust", string.Format(sut, "{0:Theyre} robust", female));
            Assert.AreEqual("He's robust", string.Format(sut, "{0:Theyre} robust", male));
            Assert.AreEqual("It's robust", string.Format(sut, "{0:Theyre} robust", neuter));
            Assert.AreEqual("They're robust", string.Format(sut, "{0:Theyre} robust", epicene));
        }

        [Test]
        public void TestUseToString()
        {
            Assert.AreEqual("Bob uses his toolbox", string.Format(sut, "{0} uses {0:their} toolbox", male));
        }
    }
}
