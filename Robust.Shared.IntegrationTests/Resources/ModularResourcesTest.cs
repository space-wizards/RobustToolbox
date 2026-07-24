using System.Globalization;
using NUnit.Framework;
using Robust.Client;
using Robust.Shared.ContentPack;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Robust.Shared.IntegrationTests.Resources;

// This test relies on Resources/manifest_mod_testing.yml
// and Robust.Shared.IntegrationTests/Resources/TestModResources existing
public sealed class ModularResourcesTest : RobustIntegrationTest
{
    // Virtual name of the resource folder in YAML
    private const string ResourceName = "EngineResourceTest";

    // Actual name of the resource folder on disk
    private const string ActualResourcePathName = "TestModResources";

    // Main types of resources
    private const string LocaleTestString = "test-string";
    private const string PrototypeTestId = "ModularResourcesTestEntity";
    private static readonly ResPath TexturePath = new("/EngineResourceTest/Textures/test_error.rsi/meta.json");

    /// <summary>
    /// Tests that other resource folders can be properly mounted from the manifest file correctly,
    /// and that the Prototypes and Localization are loaded automatically.
    /// </summary>
    [Test]
    [TestOf(typeof(ResourceManifestData))]
    public async Task TestModularFileReading()
    {
        // Override the manifest to include the TestModResources folder
        var options = new ClientIntegrationOptions
        {
            Options = new GameControllerOptions
            {
                ManifestOverride = "EngineManifests/manifest_mod_testing.yml",
                LoadContentResources = false,
                LoadConfigAndUserData = false
            }
        };

        var client = StartClient(options);
        await client.WaitIdleAsync();

        // Check that the module itself was registered correctly
        var resMan = client.ResolveDependency<IResourceManagerInternal>();

        await client.WaitPost(() =>
        {
            var manifest = ResourceManifestData.LoadResourceManifest(resMan);
            Assert.Multiple(() =>
            {
                Assert.That(manifest.ModularResources, Is.Not.Null);
                Assert.That(manifest.ModularResources!.Keys, Does.Contain(ResourceName));
                bool found = false;
                foreach (var root in resMan.GetContentRoots())
                {
                    if (root.EndsWith(ActualResourcePathName))
                        found = true;
                }

                Assert.That(found);
            });
        });

        var localeMan = client.ResolveDependency<ILocalizationManager>();
        var protoMan = client.ResolveDependency<IPrototypeManager>();

        await client.WaitPost(() =>
        {
            var culture = new CultureInfo("en-US");
            localeMan.LoadCulture(culture);
        });

        // Check that resources themselves are loaded properly and properly accessible
        Assert.Multiple(() =>
        {
            Assert.That(localeMan.HasString(LocaleTestString));
            Assert.That(protoMan.HasIndex(PrototypeTestId));
            Assert.That(resMan.ContentFileExists(TexturePath));
        });

        await client.WaitRunTicks(1);
    }

    /// <summary>
    /// Tests that manifest with a bad path pointing to an external folder throws an exception on launch.
    /// </summary>
    [Test]
    [TestOf(typeof(ResourceManifestData))]
    public async Task TestBadManifestWithExternalPath()
    {
        var options = new ClientIntegrationOptions
        {
            Options = new GameControllerOptions
            {
                ManifestOverride = "EngineManifests/manifest_mod_testing_bad.yml",
                LoadContentResources = false,
                LoadConfigAndUserData = false
            }
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var client = StartClient(options);
            await client.WaitIdleAsync();
        });
    }
}
