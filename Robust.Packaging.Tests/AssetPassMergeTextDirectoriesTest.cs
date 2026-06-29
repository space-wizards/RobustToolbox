using NUnit.Framework;
using Robust.Packaging.AssetProcessing.Passes;

namespace Robust.Packaging.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[TestOf(typeof(AssetPassMergeTextDirectories))]
internal sealed class AssetPassMergeTextDirectoriesTest
{
    [Test]
    public async Task Test()
    {
        var pass = new AssetPassMergeTextDirectories("/Prototypes", "yml", f => $"# BEGIN: {f}", f => $"# END: {f}");
        var collectorPass = AssetPassTest.SetupTestPass(pass);

        pass.InjectFileFromMemory("/Prototypes/LICENSE.txt", "Do whatever\n"u8);
        pass.InjectFileFromMemory("/Prototypes/b.yml", "# file: B\n"u8);
        pass.InjectFileFromMemory("/Prototypes/a.yml", "# file: A\n"u8);
        pass.InjectFileFromMemory("/Prototypes/z/d.yml", "# file: D\n"u8);
        pass.InjectFileFromMemory("/Prototypes/z/c.yml", "# file: C\n"u8);
        pass.InjectFinished();
        await collectorPass.FinishedTask;

        collectorPass.AssertTextFiles(
            ("/Prototypes/__merged.yml", """
                # BEGIN: /Prototypes/a.yml
                # file: A
                # END: /Prototypes/a.yml
                # BEGIN: /Prototypes/b.yml
                # file: B
                # END: /Prototypes/b.yml

                """.Replace("\r\n", "\n")),
            ("/Prototypes/z/__merged.yml", """
                # BEGIN: /Prototypes/z/c.yml
                # file: C
                # END: /Prototypes/z/c.yml
                # BEGIN: /Prototypes/z/d.yml
                # file: D
                # END: /Prototypes/z/d.yml

                """.Replace("\r\n", "\n")));
    }

    [Test]
    public async Task TestNormalizeEol()
    {
        var pass = new AssetPassMergeTextDirectories("/", "yml");
        var collectorPass = AssetPassTest.SetupTestPass(pass);

        pass.InjectFileFromMemory("/b.yml", "# file: B\r\n"u8);
        pass.InjectFileFromMemory("/a.yml", "# file: A\n"u8);
        pass.InjectFinished();
        await collectorPass.FinishedTask;

        collectorPass.AssertTextFiles(
            ("/__merged.yml", "# file: A\n# file: B\n"));
    }

    [Test]
    public async Task TestFixBom()
    {
        var pass = new AssetPassMergeTextDirectories("/", "yml");
        var collectorPass = AssetPassTest.SetupTestPass(pass);

        pass.InjectFileFromMemory("/b.yml", "\uFEFF# file: B\n"u8);
        pass.InjectFileFromMemory("/a.yml", "\uFEFF# file: A\n"u8);
        pass.InjectFinished();
        await collectorPass.FinishedTask;

        collectorPass.AssertTextFiles(
            ("/__merged.yml", "# file: A\n# file: B\n"));
    }
}
