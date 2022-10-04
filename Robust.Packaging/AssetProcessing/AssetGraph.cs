using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing;

/// <summary>
/// Used to calculate and wire dependencies in a graph of <see cref="AssetPass"/>es.
/// </summary>
public static class AssetGraph
{
    /// <summary>
    /// Wire up all dependencies in a set of asset passes.
    /// This must be called on the full set of passes before they can be properly used.
    /// </summary>
    /// <param name="passes">All the asset passes to wire up.</param>
    /// <param name="logger">Logger to assign to all asset passes, if they don't have a logger yet.</param>
    public static void CalculateGraph(IReadOnlyCollection<AssetPass> passes, IPackageLogger? logger = null)
    {
        var named = passes.ToDictionary(p => p.Name, p => p);
        // Set up dependents lists on the passes.
        foreach (var pass in passes)
        {
            pass.Logger ??= logger;

            if (pass.DependenciesList.Count == 0)
                continue;

            foreach (var dep in pass.DependenciesList)
            {
                var depPass = named[dep.Name];
                depPass.Dependents.Add(pass);

                pass.DependenciesUnfinished += 1;
            }
        }

        // Topological sort dependents lists to ensure order is correct.
        foreach (var pass in passes)
        {
            // Sort dependents with topological sorting.

            var dep = TopologicalSort.FromBeforeAfter(pass.Dependents,
                p => p.Name,
                p => p,
                p => p.DependenciesList.Single(d => d.Name == pass.Name).Before,
                p => p.DependenciesList.Single(d => d.Name == pass.Name).After,
                allowMissing: true)
                .ToArray();

            pass.Dependents = new ValueList<AssetPass>(TopologicalSort.Sort(dep));
        }
    }
}
