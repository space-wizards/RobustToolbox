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

        var rsiFolder = new ResPath("/Textures/error.rsi");
        var rsiFolderExplicit = new ResPath("/Textures/error.rsi/");
        var jsonFile = rsiFolder / "meta.json";
        var missingFile =  rsiFolder / "404FileNotFound";
        var missingFolder =  rsiFolder / "404FolderNotFound/";

        Assert.Multiple(() =>
        {
            Assert.That(resMan.ContentFileExists(jsonFile));
            Assert.That(resMan.TryContentFileRead(jsonFile, out _));
            Assert.That(resMan.ContentGetDirectoryEntries(rsiFolder).Any());
            Assert.That(resMan.ContentGetDirectoryEntries(rsiFolderExplicit).Any());
        });

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsiFolder));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(rsiFolderExplicit));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(missingFile));
            Assert.DoesNotThrow(() => resMan.ContentFileExists(missingFolder));

            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsiFolder, out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(rsiFolderExplicit, out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(missingFile, out _));
            Assert.DoesNotThrow(() => resMan.TryContentFileRead(missingFolder, out _));
        });

        Assert.Multiple(() =>
        {
            Assert.That(resMan.ContentFileExists(rsiFolder), Is.False);
            Assert.That(resMan.ContentFileExists(rsiFolderExplicit), Is.False);
            Assert.That(resMan.ContentFileExists(missingFile), Is.False);
            Assert.That(resMan.ContentFileExists(missingFolder), Is.False);

            Assert.That(resMan.TryContentFileRead(rsiFolder, out _), Is.False);
            Assert.That(resMan.TryContentFileRead(rsiFolderExplicit, out _), Is.False);
            Assert.That(resMan.TryContentFileRead(missingFile, out _), Is.False);
            Assert.That(resMan.TryContentFileRead(missingFolder , out _), Is.False);
        });
    }
}

