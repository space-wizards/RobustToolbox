using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;

namespace Robust.Packaging;

/// <summary>
/// Standard asset graph for packaging client content. Extend by wiring things up to <see cref="Input"/> and <see cref="Output"/>.
/// </summary>
/// <remarks>
/// If you want to add extra passes to run before preset passes, depend them on <see cref="Input"/> with a before of <see cref="PresetPasses"/>.
/// </remarks>
public sealed class RobustClientAssetGraph
{
    public AssetPassPipe Input { get; }
    public AssetPassPipe PresetPasses { get; }
    public AssetPassPipe Output { get; }
    public AssetPassNormalizeText NormalizeText { get; }

    /// <summary>
    /// Collection of all passes in this preset graph.
    /// </summary>
    public IReadOnlyCollection<AssetPass> AllPasses { get; }

    public RobustClientAssetGraph()
    {
        // The code injecting the list of source files is assumed to be pretty single-threaded.
        // We use a parallelizing input to break out all the work on files coming in onto multiple threads.
        Input = new AssetPassPipe { Name = "RobustClientAssetGraphInput", Parallelize = true };
        PresetPasses = new AssetPassPipe { Name = "RobustClientAssetGraphPresetPasses" };
        Output = new AssetPassPipe { Name = "RobustClientAssetGraphOutput" };
        NormalizeText = new AssetPassNormalizeText { Name = "RobustClientAssetGraphNormalizeText" };

        PresetPasses.AddDependency(Input);
        NormalizeText.AddDependency(PresetPasses).AddBefore(Output);
        Output.AddDependency(PresetPasses);
        Output.AddDependency(NormalizeText);

        AllPasses = new AssetPass[]
        {
            Input,
            PresetPasses,
            Output,
            NormalizeText
        };
    }
}
