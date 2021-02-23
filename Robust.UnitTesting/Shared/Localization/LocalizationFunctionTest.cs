using System.Globalization;
using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Localization
{
    [TestFixture]
    internal sealed class LocalizationFunctionTest
    {
        private const string FluentCode = @"
foo = { BAR($baz) }
";

        [Test]
        public void TestCustomTypes()
        {
            var ioc = new DependencyCollection();
            ioc.Register<ILocalizationManager, LocalizationManager>();
            ioc.Register<IResourceManager, ResourceManager>();
            ioc.Register<IResourceManagerInternal, ResourceManager>();
            ioc.Register<IConfigurationManager, ConfigurationManager>();
            ioc.RegisterLogs();
            ioc.BuildGraph();

            ioc.Resolve<ILogManager>().RootSawmill.AddHandler(new ConsoleLogHandler());

            var res = ioc.Resolve<IResourceManagerInternal>();
            res.MountString("/Locale/en-US/a.ftl", FluentCode);

            var loc = ioc.Resolve<ILocalizationManager>();
            var culture = new CultureInfo("en-US", false);
            loc.LoadCulture(res, culture);

            loc.AddFunction(culture, "BAR", Function);

            var ret = loc.GetString("foo", ("baz", new LocValueVector2((-7, 5))));

            Assert.That(ret, Is.EqualTo("5"));
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
    }
}
