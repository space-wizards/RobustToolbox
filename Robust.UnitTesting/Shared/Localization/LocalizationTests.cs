using System.Globalization;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Localization
{
    [TestFixture]
    internal sealed class LocalizationTests
    {
        private const string FluentCode = @"
foo = { BAR($baz) }

enum-match = { $enum ->
    [foo] A
    *[bar] B
}
";

        private (ILocalizationManager, CultureInfo) BuildLocalizationManager()
        {
            var ioc = new DependencyCollection();
            ioc.Register<ILocalizationManager, LocalizationManager>();
            ioc.Register<IResourceManager, ResourceManager>();
            ioc.Register<IResourceManagerInternal, ResourceManager>();
            ioc.Register<IConfigurationManager, ConfigurationManager>();
            ioc.RegisterLogs();
            ioc.BuildGraph();

            var res = ioc.Resolve<IResourceManagerInternal>();
            res.MountString("/Locale/en-US/a.ftl", FluentCode);

            var loc = ioc.Resolve<ILocalizationManager>();
            var culture = new CultureInfo("en-US", false);
            loc.LoadCulture(culture);

            return (loc, culture);
        }

        [Test]
        public void TestCustomTypes()
        {
            var (loc, culture) = BuildLocalizationManager();

            loc.AddFunction(culture, "BAR", Function);

            var ret = loc.GetString("foo", ("baz", new LocValueVector2((-7, 5))));

            Assert.That(ret, Is.EqualTo("5"));
        }

        [Test]
        public void TestEnumSelect()
        {
            var (loc, _) = BuildLocalizationManager();

            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Foo)), Is.EqualTo("A"));
            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Bar)), Is.EqualTo("B"));
            Assert.That(loc.GetString("enum-match", ("enum", TestEnum.Baz)), Is.EqualTo("B"));
        }

        private static ILocValue Function(LocArgs args)
        {
            return new LocValueNumber(((LocValueVector2) args.Args[0]).Value.Y);
        }

        private sealed record LocValueVector2(Vector2 Value) : LocValue<Vector2>(Value)
        {
            public override string Format(LocContext ctx)
            {
                return Value.ToString();
            }
        }

        private enum TestEnum
        {
            Foo,
            Bar,
            Baz
        }
    }
}
