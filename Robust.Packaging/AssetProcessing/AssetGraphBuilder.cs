using Robust.Shared.Collections;

namespace Robust.Packaging.AssetProcessing;

public sealed class AssetGraphDependency
{
    public readonly string Name;
    public ValueList<string> Before;
    public ValueList<string> After;

    public AssetGraphDependency(string name)
    {
        Name = name;
    }
}
