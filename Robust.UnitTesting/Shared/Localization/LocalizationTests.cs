using System;
using System.Collections.Generic;
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
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.RegisterClass<GrammarComponent>();
            componentFactory.GenerateNetIds();

            var res = IoCManager.Resolve<IResourceManagerInternal>();
            res.MountString("/Locale/en-US/a.ftl", FluentCode);
            res.MountString("/Prototypes/a.yml", YAMLCode);

            var protoMan = IoCManager.Resolve<IPrototypeManager>();

            protoMan.RegisterType(typeof(EntityPrototype));
            protoMan.LoadDirectory(new ResourcePath("/Prototypes"));
            protoMan.ResolveResults();

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

num-selector = { $num ->
    [0] A
    [1] B
    *[2] C
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

        static IEnumerable<object[]> NumericTestSource()
        {
            const byte b1 = 0, b2 = 1, b3 = 5;
            const sbyte sb1 = 0, sb2 = 1, sb3 = 5;
            const short sh1 = 0, sh2 = 1, sh3 = 5;
            const ushort ush1 = 0, ush2 = 1, ush3 = 5;
            const int i1 = 0, i2 = 1, i3 = 5;
            const uint ui1 = 0, ui2 = 1, ui3 = 5;
            const long lng1 = 0, lng2 = 1, lng3 = 5;
            const ulong ulng1 = 0, ulng2 = 1, ulng3 = 5;
            const float f1 = 0, f2 = 1, f3 = 5;
            const double d1 = 0, d2 = 1, d3 = 5;

            yield return new object[] { b1, b2, b3 };
            yield return new object[] { sb1, sb2, sb3 };
            yield return new object[] { sh1, sh2, sh3 };
            yield return new object[] { ush1, ush2, ush3 };
            yield return new object[] { i1, i2, i3 };
            yield return new object[] { ui1, ui2, ui3 };
            yield return new object[] { lng1, lng2, lng3 };
            yield return new object[] { ulng1, ulng2, ulng3 };
            yield return new object[] { f1, f2, f3 };
            yield return new object[] { d1, d2, d3 };
        }


        [Test]
        [TestCaseSource(nameof(NumericTestSource))]
        public void TestNumbers(object o1, object o2, object o3)
        {
            // Small test to check numbers are being properly converted
            var loc   = IoCManager.Resolve<ILocalizationManager>();
            var func  = new Func<object, string>(x1 => loc.GetString("num-selector", ("num", x1)));
            Assert.Multiple(() =>
            {
                Assert.That(func(o1), Is.EqualTo("A"));
                Assert.That(func(o2), Is.EqualTo("B"));
                Assert.That(func(o3), Is.EqualTo("C"));
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

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ent).EntityName, Is.EqualTo("A"));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ent).EntityDescription, Is.EqualTo("B"));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ent).EntityPrototype!.EditorSuffix, Is.EqualTo("C"));

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
