using Robust.Shared.Collections;

namespace Robust.Packaging.AssetProcessing;

public abstract class AssetPass
{
    internal int DependenciesProcessing;
    internal int JobsRunning;

    public string Name { get; set; }

    internal ValueList<AssetPass> Dependents;

    public ValueList<AssetGraphDependency> Dependencies;

    public AssetPass()
    {
        Name = GetType().Name;
    }

    public void SendFile(AssetFile file)
    {
        foreach (var dependent in Dependents)
        {
            var result = dependent.InternalAcceptFile(file);
            if (result)
                return;
        }
    }

    public void SendFinished()
    {
        // Console.WriteLine($"{Name}: Finished");

        foreach (var dependent in Dependents)
        {
            dependent.DecrementFinished();
        }
    }

    public void DecrementFinished()
    {
        var newVal = Interlocked.Decrement(ref DependenciesProcessing);
        if (newVal == 0)
        {
            AcceptFinished();
        }
    }

    public virtual void AcceptFinished()
    {
        SendFinished();
    }

    public virtual AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        return AssetFileAcceptResult.Pass;
    }

    internal bool InternalAcceptFile(AssetFile file)
    {
        // Console.WriteLine($"{Name}: Accepting {file.Path}");

        var result = AcceptFile(file);
        return result != AssetFileAcceptResult.Pass;
    }
}

public sealed class AssetPassRoot : AssetPass
{

}

public enum AssetFileAcceptResult
{
    Pass,
    Using,
    Consumed,
}
