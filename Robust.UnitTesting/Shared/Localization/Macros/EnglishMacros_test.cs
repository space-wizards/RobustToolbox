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

        private readonly Subject female = new("Lisa", Gender.Female, true);
        private readonly Subject male = new("Bob", Gender.Male, true);
        private readonly Subject epicene = new("Michel", Gender.Epicene, true);
        private readonly Subject neuter = new("D.O.O.R.K.N.O.B.", Gender.Neuter, true);

        public void TestThey()
        {
            ITextMacro sut = new They();
            Assert.That(sut.CapitalizedFormat(female), Is.EqualTo("She"));
            Assert.That(sut.Format(male), Is.EqualTo("he"));
            Assert.That(sut.Format(epicene), Is.EqualTo("they"));
            Assert.That(sut.Format(neuter), Is.EqualTo("it"));
        }

        [Test]
        public void TestTheir()
        {
            var sut = new Their();
            Assert.That(sut.Format(female), Is.EqualTo("her"));
            Assert.That(sut.Format(male), Is.EqualTo("his"));
            Assert.That(sut.Format(epicene), Is.EqualTo("their"));
            Assert.That(sut.Format(neuter), Is.EqualTo("its"));
        }

        [Test]
        public void TestTheirs()
        {
            var sut = new Theirs();
            Assert.That(sut.Format(female), Is.EqualTo("hers"));
            Assert.That(sut.Format(male), Is.EqualTo("his"));
            Assert.That(sut.Format(epicene), Is.EqualTo("theirs"));
            Assert.That(sut.Format(neuter), Is.EqualTo("its"));
        }

        [Test]
        public void TestThem()
        {
            var sut = new Them();
            Assert.That(sut.Format(female), Is.EqualTo("her"));
            Assert.That(sut.Format(male), Is.EqualTo("him"));
            Assert.That(sut.Format(epicene), Is.EqualTo("them"));
            Assert.That(sut.Format(neuter), Is.EqualTo("it"));
        }

        [Test]
        public void TestThemself()
        {
            var sut = new Themself();
            Assert.That(sut.Format(female), Is.EqualTo("herself"));
            Assert.That(sut.Format(male), Is.EqualTo("himself"));
            Assert.That(sut.Format(epicene), Is.EqualTo("themself"));
            Assert.That(sut.Format(neuter), Is.EqualTo("itself"));
        }

        [Test]
        public void TestTheyre()
        {
            ITextMacro sut = new Theyre();
            Assert.That(sut.CapitalizedFormat(female), Is.EqualTo("She's"));
            Assert.That(sut.Format(male), Is.EqualTo("he's"));
            Assert.That(sut.Format(epicene), Is.EqualTo("they're"));
            Assert.That(sut.Format(neuter), Is.EqualTo("it's"));
        }

        [Test]
        public void TestTheName()
        {
            var cpu = new Subject("CPU", Gender.Neuter, false);
            ITextMacro sut = new TheName();
            Assert.That(sut.CapitalizedFormat(cpu), Is.EqualTo("The CPU"));
            Assert.That(sut.Format(cpu), Is.EqualTo("the CPU"));
            Assert.That(sut.Format(male), Is.EqualTo(male.Name));
        }
    }
}
