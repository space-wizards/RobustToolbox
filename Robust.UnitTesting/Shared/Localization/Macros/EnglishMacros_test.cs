using NUnit.Framework;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Localization.Macros.English;

namespace Robust.UnitTesting.Shared.Localization.Macros
{
    [TestFixture, Parallelizable]
    public class EnglishMacros_test
    {
        private struct Subject : IGenderable, IProperNamable
        {
            public string Name;

            public Gender Gender { get; set; }

            public bool Proper { get; set; }

            public Subject(string name, Gender gender, bool proper)
            {
                Name = name;
                Gender = gender;
                Proper = proper;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private readonly Subject female = new Subject("Lisa", Gender.Female, true);
        private readonly Subject male = new Subject("Bob", Gender.Male, true);
        private readonly Subject epicene = new Subject("Michel", Gender.Epicene, true);
        private readonly Subject neuter = new Subject("D.O.O.R.K.N.O.B.", Gender.Neuter, true);

        public void TestThey()
        {
            ITextMacro sut = new They();
            Assert.AreEqual("She", sut.CapitalizedFormat(female));
            Assert.AreEqual("he", sut.Format(male));
            Assert.AreEqual("they", sut.Format(epicene));
            Assert.AreEqual("it", sut.Format(neuter));
        }

        [Test]
        public void TestTheir()
        {
            var sut = new Their();
            Assert.AreEqual("her", sut.Format(female));
            Assert.AreEqual("his", sut.Format(male));
            Assert.AreEqual("their", sut.Format(epicene));
            Assert.AreEqual("its", sut.Format(neuter));
        }

        [Test]
        public void TestTheirs()
        {
            var sut = new Theirs();
            Assert.AreEqual("hers", sut.Format(female));
            Assert.AreEqual("his", sut.Format(male));
            Assert.AreEqual("theirs", sut.Format(epicene));
            Assert.AreEqual("its", sut.Format(neuter));
        }

        [Test]
        public void TestThem()
        {
            var sut = new Them();
            Assert.AreEqual("her", sut.Format(female));
            Assert.AreEqual("him", sut.Format(male));
            Assert.AreEqual("them", sut.Format(epicene));
            Assert.AreEqual("it", sut.Format(neuter));
        }

        [Test]
        public void TestThemself()
        {
            var sut = new Themself();
            Assert.AreEqual("herself", sut.Format(female));
            Assert.AreEqual("himself", sut.Format(male));
            Assert.AreEqual("themself", sut.Format(epicene));
            Assert.AreEqual("itself", sut.Format(neuter));
        }

        [Test]
        public void TestTheyre()
        {
            ITextMacro sut = new Theyre();
            Assert.AreEqual("She's", sut.CapitalizedFormat(female));
            Assert.AreEqual("he's", sut.Format(male));
            Assert.AreEqual("they're", sut.Format(epicene));
            Assert.AreEqual("it's", sut.Format(neuter));
        }

        [Test]
        public void TestTheName()
        {
            var cpu = new Subject("CPU", Gender.Neuter, false);
            ITextMacro sut = new TheName();
            Assert.AreEqual("The CPU", sut.CapitalizedFormat(cpu));
            Assert.AreEqual("the CPU", sut.Format(cpu));
            Assert.AreEqual(male.Name, sut.Format(male));
        }
    }
}
