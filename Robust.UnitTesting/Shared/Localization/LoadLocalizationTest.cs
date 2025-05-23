using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Robust.UnitTesting.Shared.Localization;

[TestFixture]
public sealed class LoadLocalizationTest : RobustUnitTest
{
    private const string DuplicateTerm = @"
term1 = 1
term1 = 2
";
    protected override void OverrideIoC()
    {
        base.OverrideIoC();

        IoCManager.Register<ILogManager, SpyLogManager>(overwrite: true);
    }


    [Test]
    public void TestLoadLocalization()
    {
        var res = IoCManager.Resolve<IResourceManagerInternal>();
        res.MountString("/Locale/en-US/a.ftl", DuplicateTerm);

        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.Initialize();

        var spyLog = (SpyLogManager) IoCManager.Resolve<ILogManager>();
        var culture = new CultureInfo("en-US", false);

        var x = spyLog.CountError;
        loc.LoadCulture(culture);
        Assert.That(spyLog.CountError, NUnit.Framework.Is.GreaterThan(x));
    }
}

