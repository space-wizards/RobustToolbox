using System.Globalization;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Localization
{
    [TestFixture]
    internal sealed class LocalizationTests : RobustUnitTest
    {

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            IoCManager.Resolve<IComponentFactory>().RegisterClass<GrammarComponent>();

            var res = IoCManager.Resolve<IResourceManagerInternal>();
            res.MountString("/Locale/en-US/a.ftl", FluentCode);
            res.MountString("/Prototypes/a.yml", YAMLCode);

            IoCManager.Resolve<IPrototypeManager>().LoadDirectory(new ResourcePath("/Prototypes"));

            var loc = IoCManager.Resolve<ILocalizationManager>();
            var culture = new CultureInfo("en-US", false);
            loc.LoadCulture(culture);
        }

        private const string YAMLCode = @"
# Values specified in prototype
- type: entity
  id: PropsInPrototype
  name: A
  description: B
  suffix: C
  loc:
    gender: male
    proper: true

# Values specified in fluent
- type: entity
  id: PropsInLoc

# Values specified in YAML but overriden by fluent.
- type: entity
  id: PropsInLocOverriding
  name: XA
  description: XB
  suffix: XC
  loc:
    gender: female
    proper: false

# Values specified in fluent at PARENT and replaced by child YAML.
- type: entity
  id: TestInheritOverridingParent

- type: entity
  id: TestInheritOverridingChild
  parent: TestInheritOverridingParent
  name: A
  description: B
  suffix: C
  loc:
    gender: male
    proper: true

# Attribures stored in grammar component
- type: entity
  id: PropsInGrammar
  name: A
  description: B
  suffix: C
  loc:
    gender: female
    proper: false

  components:
  - type: Grammar
    attributes:
      gender: male
      proper: true


- type: entity
  id: GenderTestEntityNoComp

- type: entity
  id: GenderTestEntityWithComp
  components:
  - type: Grammar
    attributes:
      gender: Female
";

        private const string FluentCode = @"
enum-match = { $enum ->
    [foo] A
    *[bar] B
}

ent-GenderTestEntityNoComp = Gender Test Entity
  .gender = male
  .otherAttrib = sausages

ent-GenderTestEntityWithComp = Gender Test Entity 2

ent-PropsInLoc = A
  .desc = B
  .suffix = C
  .gender = male
  .proper = true

ent-PropsInLocOverriding = A
  .desc = B
  .suffix = C
  .gender = male
  .proper = true

ent-TestInheritOverridingParent = XA
  .desc = XB
  .suffix = XC
  .gender = female
  .proper = false


test-message-gender = { GENDER($entity) ->
  [male] male
  [female] female
  *[neuter] BAD
}

test-message-proper = { PROPER($entity) ->
  [true] true
  *[false] false
}

test-message-custom-attrib = { ATTRIB($entity, ""otherAttrib"") }
";

        [Test]
        public void TestEnumSelect()
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Foo)), Is.EqualTo("A"));
            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Bar)), Is.EqualTo("B"));
            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Baz)), Is.EqualTo("B"));
        }

        [Test]
        public void TestCustomFunctions()
        {
            var entMan          = IoCManager.Resolve<IEntityManager>();
            var testEntNoComp   = entMan.CreateEntityUninitialized("GenderTestEntityNoComp");
            var testEntWithComp = entMan.CreateEntityUninitialized("GenderTestEntityWithComp");

            var loc               = IoCManager.Resolve<ILocalizationManager>();
            var genderFromAttrib  = loc.GetString("test-message-gender", ("entity", testEntNoComp));
            var genderFromGrammar = loc.GetString("test-message-gender", ("entity", testEntWithComp));
            var customAttrib      = loc.GetString("test-message-custom-attrib", ("entity", testEntNoComp));

            Assert.Multiple(() =>
            {
                Assert.That(genderFromAttrib, Is.EqualTo("male"));
                Assert.That(genderFromGrammar, Is.EqualTo("female"));
                Assert.That(customAttrib, Is.EqualTo("sausages"));
            });
        }

        [Test]
        [TestCase("PropsInPrototype")]
        [TestCase("PropsInLoc")]
        [TestCase("PropsInLocOverriding")]
        [TestCase("PropsInGrammar")]
        [TestCase("TestInheritOverridingChild")]
        public void TestLocData(string prototype)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            var entMan = IoCManager.Resolve<IEntityManager>();
            var ent = entMan.CreateEntityUninitialized(prototype);

            Assert.That(ent.Name, Is.EqualTo("A"));
            Assert.That(ent.Description, Is.EqualTo("B"));
            Assert.That(ent.Prototype!.EditorSuffix, Is.EqualTo("C"));

            Assert.That(loc.GetString("test-message-gender", ("entity", ent)), Is.EqualTo("male"));
            Assert.That(loc.GetString("test-message-proper", ("entity", ent)), Is.EqualTo("true"));
        }

        private enum TestEnum
        {
            Foo,
            Bar,
            Baz
        }
    }
}
