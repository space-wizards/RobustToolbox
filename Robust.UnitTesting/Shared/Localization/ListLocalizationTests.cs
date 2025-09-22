using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Localization;

[TestFixture]
internal sealed class ListLocalizationTests : RobustUnitTest
{
    protected override Type[] ExtraComponents => new[] {typeof(GrammarComponent)};

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        var res = IoCManager.Resolve<IResourceManagerInternal>();
        res.MountString("/Locale/en-US/a.ftl", EnglishFluentCode);
        res.MountString("/Locale/es-ES/a.ftl", SpanishFluentCode);
        res.MountString("/Locale/supplemental/gender.json", SupplementalData);
        res.MountString("/Locale/en/listPatterns.json", EnglishListPatterns);
        res.MountString("/Locale/es/listPatterns.json", SpanishListPatterns);
        res.MountString("/EnginePrototypes/peopleNames.yml", YAMLCode);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        protoMan.RegisterKind(typeof(EntityPrototype), typeof(EntityCategoryPrototype));
        protoMan.LoadDirectory(new ResPath("/EnginePrototypes"));
        protoMan.ResolveResults();

        var loc = IoCManager.Resolve<ILocalizationManager>();

        loc.Initialize();

        loc.LoadCulture(new CultureInfo("en-US", false));
        loc.LoadCulture(new CultureInfo("es-ES", false));
    }

    static IEnumerable<object[]> TestGenderFallbackCohesionData()
    {
        yield return new object[] { "en-US", "es-ES", new[] { "Alice", "Lucia" }, "spanish-only-gender", "female" };
        yield return new object[] { "es-ES", "en-US", new[] { "Alice", "Bob" }, "english-only-gender", "epicene" };
    }

    [Test]
    [TestCaseSource(nameof(TestGenderFallbackCohesionData))]
    public void TestGenderFallbackCohesion(string locale, string fallback, string[] people, string messageId, string expectedPattern)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.SetCulture(new CultureInfo(locale, false));
        loc.SetFallbackCluture(new CultureInfo(fallback, false));

        var list = new List<EntityUid>();
        foreach (var person in people)
        {
            list.Add(entMan.CreateEntityUninitialized(person));
        }

        var pattern = loc.GetString(messageId, ("item", list));
        Assert.That(pattern, Is.EqualTo(expectedPattern));
    }

    static IEnumerable<object[]> TestListFallbackCohesionData()
    {
        yield return new object[] { "en-US", "es-ES", new[] { "Alice", "Lucia", "Bob" }, "spanish-only-list", "Alice, Lucia y Bob" };
        yield return new object[] { "es-ES", "en-US", new[] { "Alice", "Lucia", "Bob" }, "english-only-list", "Alice, Lucia, and Bob" };
    }

    [Test]
    [TestCaseSource(nameof(TestListFallbackCohesionData))]
    public void TestListFallbackCohesion(string locale, string fallback, string[] people, string messageId, string expectedPattern)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.SetCulture(new CultureInfo(locale, false));
        loc.SetFallbackCluture(new CultureInfo(fallback, false));

        var list = new List<EntityUid>();
        foreach (var person in people)
        {
            list.Add(entMan.CreateEntityUninitialized(person));
        }

        var pattern = loc.GetString(messageId, ("item", list));
        Assert.That(pattern, Is.EqualTo(expectedPattern));
    }

    static IEnumerable<object[]> TestPatternData()
    {
        yield return new object[] { "en-US", new[] { "Alice" }, "Alice" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia" }, "Alice and Lucia" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia", "Bob" }, "Alice, Lucia, and Bob" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia", "Bob", "Miguel", "Rock", "Roca", "Edgar", "Marge" }, "Alice, Lucia, Bob, Miguel, Rock, Roca, Edgar, and Marge" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia", "Bob" }, "Alice, Lucia, and Bob" };
        yield return new object[] { "es-ES", new[] { "Alice" }, "Alice" };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia" }, "Alice y Lucia" };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia", "Bob" }, "Alice, Lucia y Bob" };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia", "Bob", "Miguel", "Rock", "Roca", "Edgar", "Marge" }, "Alice, Lucia, Bob, Miguel, Rock, Roca, Edgar y Marge" };
    }

    [Test]
    [TestCaseSource(nameof(TestPatternData))]
    public void TestListPatterns(string locale, string[] people, string expectedPattern)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.SetCulture(new CultureInfo(locale, false));

        var list = new List<EntityUid>();
        foreach (var person in people)
        {
            list.Add(entMan.CreateEntityUninitialized(person));
        }

        var pattern = loc.GetString("list-pattern", ("item", list));
        Assert.That(pattern, Is.EqualTo(expectedPattern));
    }

    static IEnumerable<object[]> TestGenderData()
    {
        yield return new object[] { "en-US", new[] { "Alice" }, "female" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia" }, "epicene" };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia", "Bob" }, "epicene" };
        yield return new object[] { "es-ES", new[] { "Alice" }, "female" };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia" }, "female" };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia", "Bob" }, "male" };
        yield return new object[] { "es-ES", new[] { "Bob" }, "male" };
        yield return new object[] { "es-ES", new[] { "Rock", "Roca" }, "neuter" };
        yield return new object[] { "es-ES", new[] { "Edgar", "Marge" }, "epicene" };
    }

    [Test]
    [TestCaseSource(nameof(TestGenderData))]
    public void TestListGender(string locale, string[] people, string expectedGender)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.SetCulture(new CultureInfo(locale, false));

        var list = new List<EntityUid>();
        foreach (var person in people)
        {
            list.Add(entMan.CreateEntityUninitialized(person));
        }

        var gender = loc.GetString("list-gender", ("item", list));

        Assert.That(gender, Is.EqualTo(expectedGender));
    }

    static IEnumerable<object[]> TestLocalizationData()
    {
        yield return new object[] { "en-US", new[] { "Alice" }, "Alice is red.", "Alice is kissing herself." };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia" }, "Alice and Lucia are red.", "Alice and Lucia are kissing each other." };
        yield return new object[] { "en-US", new[] { "Alice", "Lucia", "Bob" }, "Alice, Lucia, and Bob are red.", "Alice, Lucia, and Bob are kissing each other." };
        yield return new object[] { "es-ES", new[] { "Alice" }, "Alice es roja.", "Alice se besa." };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia" }, "Alice y Lucia son rojas.", "Alice y Lucia se besan." };
        yield return new object[] { "es-ES", new[] { "Alice", "Lucia", "Bob" }, "Alice, Lucia y Bob son rojos.", "Alice, Lucia y Bob se besan." };
        yield return new object[] { "es-ES", new[] { "Bob" }, "Bob es rojo.", "Bob se besa." };
        yield return new object[] { "es-ES", new[] { "Rock", "Roca" }, "Rock y Roca son rojes.", "Rock y Roca se besan." };
        yield return new object[] { "es-ES", new[] { "Roca" }, "Roca es roje.", "Roca se besa." };
        yield return new object[] { "es-ES", new[] { "Alice", "Bob" }, "Alice y Bob son rojos.", "Alice y Bob se besan." };
    }

    [Test]
    [TestCaseSource(nameof(TestLocalizationData))]
    [Ignore("Linguini improperly handles functions & causes matching to fail on COUNT()", Until = "2025-10-21")]
    public void TestListLocalization(string locale, string[] people, string expectedColor, string expectedReciprocal)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.SetCulture(new CultureInfo(locale, false));

        var list = new List<EntityUid>();
        foreach (var person in people)
        {
            list.Add(entMan.CreateEntityUninitialized(person));
        }

        var listConjugation = loc.GetString("list-gender", ("item", list));
        var reciprocalConjugation = loc.GetString("list-reciprocal", ("item", list));

        Assert.That(listConjugation, Is.EqualTo(expectedColor));
        Assert.That(reciprocalConjugation, Is.EqualTo(expectedReciprocal));
    }

    private const string YAMLCode = @"
- type: entity
  id: Alice
  name: Alice
  loc:
    gender: female

- type: entity
  id: Lucia
  name: Lucia
  loc:
    gender: female

- type: entity
  id: Bob
  name: Bob
  loc:
    gender: male

- type: entity
  id: Miguel
  name: Miguel
  loc:
    gender: male

- type: entity
  id: Rock
  name: Rock
  loc:
    gender: neuter

- type: entity
  id: Roca
  name: Roca
  loc:
    gender: neuter

- type: entity
  id: Edgar
  name: Edgar
  loc:
    gender: epicene

- type: entity
  id: Marge
  name: Marge
  loc:
    gender: epicene
";

    private const string EnglishFluentCode = """
zzzz-conjugate-be = { GENDER($ent) ->
    [epicene] are
   *[other] is
   }

zzzz-reflexive-pronoun = { GENDER($ent) ->
    [male] himself
    [female] herself
    [epicene] themselves
   *[neuter] itself
   }

zzzz-reciprocal-pronoun = { COUNT($ent) ->
    [one] {REFLEXIVE($ent)}
   *[other] each other
   }

list-conjugation = { LIST($item) } { CONJUGATE-BE($item) } red.
list-reciprocal = { LIST($item) } { CONJUGATE-BE($item) } kissing { RECIPROCAL($item) }.
list-gender = { GENDER($item) }
list-pattern = { LIST($item) }
english-only-list = { LIST($item) }
english-only-gender = { GENDER($item) }
""";

    private const string SpanishFluentCode = """
zzzz-conjugate-be = { COUNT($ent) ->
    [one] es
   *[other] son
}
zzzz-reciprocal-pronoun = se
-conjugate-red = { COUNT($ent) ->
    [one] { GENDER($ent) ->
        [male] rojo
        [female] roja
       *[other] roje
    }
   *[other] { GENDER($ent) ->
        [male] rojos
        [female] rojas
       *[other] rojes
    }
}
list-conjugation = { LIST($item) } { CONJUGATE-BE($item) } { -conjugate-red(ent: $item) }.
list-reciprocal = { LIST($item) } { RECIPROCAL($item) } { COUNT($item) ->
    [one] besa
   *[other] besan
}.
list-gender = { GENDER($item) }
list-pattern = { LIST($item) }
spanish-only-list = { LIST($item) }
spanish-only-gender = { GENDER($item) }
""";

    private const string SupplementalData = """
{
  "supplemental": {
    "gender": {
      "personList": {
        "es": "maleTaints",
        "en": "neutral"
      }
    }
  }
}
""";

    private const string EnglishListPatterns = """
{
  "main": {
    "en": {
      "identity": {
        "language": "en"
      },
      "listPatterns": {
        "listPattern-type-standard": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, and {1}",
          "2": "{0} and {1}"
        },
        "listPattern-type-or": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, or {1}",
          "2": "{0} or {1}"
        },
        "listPattern-type-or-narrow": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, or {1}",
          "2": "{0} or {1}"
        },
        "listPattern-type-or-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, or {1}",
          "2": "{0} or {1}"
        },
        "listPattern-type-standard-narrow": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, {1}",
          "2": "{0}, {1}"
        },
        "listPattern-type-standard-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, & {1}",
          "2": "{0} & {1}"
        },
        "listPattern-type-unit": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, {1}",
          "2": "{0}, {1}"
        },
        "listPattern-type-unit-narrow": {
          "start": "{0} {1}",
          "middle": "{0} {1}",
          "end": "{0} {1}",
          "2": "{0} {1}"
        },
        "listPattern-type-unit-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, {1}",
          "2": "{0}, {1}"
        }
      }
    }
  }
}
""";

    private const string SpanishListPatterns = """
{
  "main": {
    "es": {
      "identity": {
        "language": "es"
      },
      "listPatterns": {
        "listPattern-type-standard": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} y {1}",
          "2": "{0} y {1}"
        },
        "listPattern-type-or": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} o {1}",
          "2": "{0} o {1}"
        },
        "listPattern-type-or-narrow": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} o {1}",
          "2": "{0} o {1}"
        },
        "listPattern-type-or-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} o {1}",
          "2": "{0} o {1}"
        },
        "listPattern-type-standard-narrow": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} y {1}",
          "2": "{0} y {1}"
        },
        "listPattern-type-standard-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} y {1}",
          "2": "{0} y {1}"
        },
        "listPattern-type-unit": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0} y {1}",
          "2": "{0} y {1}"
        },
        "listPattern-type-unit-narrow": {
          "start": "{0} {1}",
          "middle": "{0} {1}",
          "end": "{0} {1}",
          "2": "{0} {1}"
        },
        "listPattern-type-unit-short": {
          "start": "{0}, {1}",
          "middle": "{0}, {1}",
          "end": "{0}, {1}",
          "2": "{0} y {1}"
        }
      }
    }
  }
}
""";
}
