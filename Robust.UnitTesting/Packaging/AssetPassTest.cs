using NUnit.Framework;
using Robust.Packaging.AssetProcessing;

namespace Robust.UnitTesting.Packaging;

/// <summary>
/// Helper class for testing <see cref="AssetPass"/>.
/// </summary>
public static class AssetPassTest
{
    /// <summary>
    /// Make an asset pass write into a <see cref="AssetPassTestCollector"/> and resolve the graph.
    /// </summary>
    /// <remarks>
    /// The resolved graph logs to the NUnit test context.
    /// </remarks>
    public static AssetPassTestCollector SetupTestPass(AssetPass testedPass)
    {
        var logger = new PackageLoggerNUnit(TestContext.Out);
        var collectorPass = new AssetPassTestCollector();

        collectorPass.AddDependency(testedPass);

        AssetGraph.CalculateGraph([testedPass, collectorPass], logger);

        return collectorPass;
    }
}
