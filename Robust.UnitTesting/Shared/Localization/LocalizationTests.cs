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
            IoCManager.Resolve<IComponentFactory>().Register<GrammarComponent>();

            var res = IoCManager.Resolve<IResourceManagerInternal>();
            res.MountString("/Locale/en-US/a.ftl", FluentCode);
            res.MountString("/Prototypes/a.yml", YAMLCode);

            IoCManager.Resolve<IPrototypeManager>().LoadDirectory(new ResourcePath("/Prototypes"));

            var loc = IoCManager.Resolve<ILocalizationManager>();
            var culture = new CultureInfo("en-US", false);
            loc.LoadCulture(culture);
        }

        private const string YAMLCode = @"

- type: entity
  id: SpecifiedInProto
  localizationId: 'not-auto-kebab'

- type: entity
  id: SpecifiedInComponent
  components:
    - type: Grammar
      localizationId: 'from-component'

- type: entity
  id: SpecifiedAuto

- type: entity
  id: GenderTestEntityNoComp

- type: entity
  id: GenderTestEntityWithComp
  components:
  - type: Grammar
    gender: Female
";

        private const string FluentCode = @"

enum-match = { $enum ->
    [foo] A
    *[bar] B
}

ent-gender-test-entity-no-comp = Gender Test Entity
  .gender = male
  .otherAttrib = sausages

ent-gender-test-entity-with-comp = Gender Test Entity 2

test-message-gender = { GENDER($entity) ->
  [male] male
  [female] female
  *[other] BAD
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
        public void TestLocalizationID()
        {
            var entMan               = IoCManager.Resolve<IEntityManager>();
            var specifiedInProto     = entMan.CreateEntityUninitialized("SpecifiedInProto");
            var specifiedInComponent = entMan.CreateEntityUninitialized("SpecifiedInComponent");
            var specifiedAuto        = entMan.CreateEntityUninitialized("SpecifiedAuto");

            System.Console.WriteLine(specifiedInProto.Prototype?.LocalizationID);
            System.Console.WriteLine(specifiedInComponent.GetComponent<GrammarComponent>().LocalizationId);
            System.Console.WriteLine(specifiedAuto.Prototype?.LocalizationID);

            Assert.Multiple(() =>
            {
                Assert.That(specifiedInProto.Prototype?.LocalizationID, Is.EqualTo("not-auto-kebab"));
                Assert.That(specifiedInComponent.GetComponent<GrammarComponent>().LocalizationId, Is.EqualTo("from-component"));
                Assert.That(specifiedAuto.Prototype?.LocalizationID, Is.EqualTo("ent-specified-auto"));
            });
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

        private enum TestEnum
        {
            Foo,
            Bar,
            Baz
        }
    }
}
