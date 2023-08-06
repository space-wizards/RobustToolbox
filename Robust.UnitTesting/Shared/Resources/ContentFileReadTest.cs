using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Resources;

public sealed class ContentFileReadTest : RobustIntegrationTest
{
    [Test]
    [TestOf(typeof(ResourceManager))]
    public async Task TestFileReading()
    {
        var client = StartClient();
        await client.WaitIdleAsync();

        var resMan = client.ResolveDependency<IResourceManager>();

        // This tests relies on /Textures/error.rsi existing.

        var rsi = new ResPath("/Textures/error.rsi");
        var rsi2 = new ResPath("/Textures/error.rsi/");
        var metaFile = rsi / "meta.json";

        Assert.That(resMan.ContentFileExists(metaFile));
        Assert.That(resMan.TryContentFileRead(metaFile, out _));
        Assert.That(resMan.ContentGetDirectoryEntries(rsi).Any());
        Assert.That(resMan.ContentGetDirectoryEntries(rsi2).Any());

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsi));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsi2));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsi / "someGarbageFile"));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsi / "someGarbageFolder/"));

            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsi, out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsi2, out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsi / "someGarbageFile", out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsi / "someGarbageFolder/", out _));
        });

        Assert.Multiple(() =>
        {
            Assert.That(resMan.ContentFileExists(rsi), Is.False);
            Assert.That(resMan.ContentFileExists(rsi2), Is.False);
            Assert.That(resMan.ContentFileExists(rsi / "someGarbageFile"), Is.False);
            Assert.That(resMan.ContentFileExists(rsi / "someGarbageFolder/"), Is.False);

            Assert.That(resMan.TryContentFileRead(rsi, out _), Is.False);
            Assert.That(resMan.TryContentFileRead(rsi2, out _), Is.False);
            Assert.That(resMan.TryContentFileRead(rsi / "someGarbageFile", out _), Is.False);
            Assert.That(resMan.TryContentFileRead(rsi / "someGarbageFolder/" , out _), Is.False);
        });
    }
}

