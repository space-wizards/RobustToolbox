using NUnit.Framework;
using Robust.Client;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Robust.Shared.IntegrationTests.Resources;

// This test relies on Resources/manifest_mod_testing.yml
// and Robust.Shared.IntegrationTests/Resources/TestModResources existing
public sealed class ModularResourcesTest : RobustIntegrationTest
{
    private const string ResourceName = "EngineResourceTest";
    private const string LocaleTestString = "test-string";
    private const string PrototypeTestId = "ModularResourcesTestEntity";
    private static readonly ResPath TexturePath = new("/EngineResourceTest/Textures/test_error.rsi/meta.json");

    /// <summary>
    /// Tests that other resource folders can be properly mounted from the manifest file correctly,
    /// and that the Prototypes and Localization are loaded automatically.
    /// </summary>
    [Test]
    [TestOf(typeof(ResourceManager))]
    public async Task TestModularFileReading()
    {
        // Override the manifest to include the TestModResources folder
        var options = new ClientIntegrationOptions
        {
            Options = new GameControllerOptions
            {
                ManifestOverride = "manifest_mod_testing.yml",
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
                Assert.That(resMan.GetContentRoots(), Does.Contain(new ResPath(ResourceName).ToRootedPath().ToString()));
            });
        });

        var localeMan = client.ResolveDependency<ILocalizationManager>();
        var protoMan = client.ResolveDependency<IPrototypeManager>();

        // Check that resources themselves are loaded properly and properly accessible
        Assert.Multiple(() =>
        {
            Assert.That(localeMan.HasString(LocaleTestString));
            Assert.That(protoMan.HasIndex(PrototypeTestId));
            Assert.That(resMan.ContentFileExists(TexturePath));
        });

        // Spawn a test entity in nullspace for good measure.
        var cEntMan = client.ResolveDependency<IEntityManager>();

        await client.WaitPost(() =>
        {
            cEntMan.Spawn(PrototypeTestId);
        });

        await client.WaitRunTicks(1);
    }
}
