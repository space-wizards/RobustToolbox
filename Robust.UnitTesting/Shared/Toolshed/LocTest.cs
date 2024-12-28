using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Localization;
using Robust.Shared.Toolshed;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed class LocTest : ToolshedTest
{
    [Test]
    public async Task AllCommandsHaveDescriptions()
    {
        var locMan = Server.ResolveDependency<ILocalizationManager>();
        var toolMan = Server.ResolveDependency<ToolshedManager>();
        var locStrings = new HashSet<string>();

        // Its neat that you can mostly do this via toolshed and all, but I'm still gonna turn it into a "real" test.
        // Assert.That(InvokeCommand("cmd:list where { cmd:descloc loc:tryloc isnull }", out var res));
        // Assert.That((IEnumerable<CommandSpec>)res!, Is.Empty, "All commands must have localized descriptions.");

        var testAssembly = typeof(LocTest).Assembly;

        await Server.WaitAssertion(() =>
        {
            locMan.LoadCulture(new CultureInfo("en-US"));

            Assert.Multiple(() =>
            {
                foreach (var cmd in toolMan.DefaultEnvironment.AllCommands())
                {
                    if (cmd.Cmd.GetType().Assembly == testAssembly)
                        continue;

                    var descLoc = cmd.DescLocStr();
                    Assert.That(locStrings.Add(descLoc), $"Duplicate command description key: {descLoc}");
                    Assert.That(locMan.TryGetString(descLoc, out _), $"Failed to get command description for command {cmd.FullName()}");
                }
            });
        });
    }
}
