using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Toolshed;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed class LocTest : ToolshedTest
{
    [Test]
    public async Task AllCommandsHaveDescriptions()
    {
        await Server.WaitAssertion(() =>
        {
            IoCManager.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));

            Assert.That(InvokeCommand("cmd:list where { cmd:descloc loc:tryloc isnull }", out var res));
            Assert.That((IEnumerable<CommandSpec>)res!, Is.Empty, "All commands must have localized descriptions.");
        });
    }
}
