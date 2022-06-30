using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Packaging.AssetProcessing;

// Originally I was gonna have a central mutable pile of assets. Yeah that was a spaghetti idea.
// Anyways, please eternalize the name I came up with for it: the file pile

public sealed class AssetGraph
{
    private ValueList<AssetPass> _passes;

    public void AddPass(AssetPass pass)
    {
        _passes.Add(pass);
    }

    public void CalculateGraph()
    {
        var named = _passes.ToDictionary(p => p.Name, p => p);
        foreach (var pass in _passes)
        {
            if (pass.Dependencies.Count == 0)
                continue;

            foreach (var dep in pass.Dependencies)
            {
                var depPass = named[dep.Name];
                depPass.Dependents.Add(pass);

                pass.DependenciesProcessing += 1;
            }
        }

        foreach (var pass in _passes)
        {
            // Sort dependents with topological sorting.

            var dep = TopologicalSort.FromBeforeAfter(pass.Dependents,
                p => p.Name,
                p => p,
                p => p.Dependencies.Single(d => d.Name == pass.Name).Before,
                p => p.Dependencies.Single(d => d.Name == pass.Name).After).ToList();

            pass.Dependents = new ValueList<AssetPass>(TopologicalSort.Sort(dep));
        }
    }
}
