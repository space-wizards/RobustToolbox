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
    public AssetPassMergeTextDirectories MergePrototypeDirectories { get; }
    public AssetPassMergeTextDirectories MergeLocaleDirectories { get; }
    public AssetPassPackRsis PackRsis { get; }

    /// <summary>
    /// Collection of all passes in this preset graph.
    /// </summary>
    public IReadOnlyCollection<AssetPass> AllPasses { get; }

    /// <param name="parallel">Should inputs be run in parallel. Should only be turned off for debugging.</param>
    public RobustClientAssetGraph(bool parallel = true)
    {
        // The code injecting the list of source files is assumed to be pretty single-threaded.
        // We use a parallelizing input to break out all the work on files coming in onto multiple threads.
        Input = new AssetPassPipe { Name = "RobustClientAssetGraphInput", Parallelize = parallel };
        PresetPasses = new AssetPassPipe { Name = "RobustClientAssetGraphPresetPasses" };
        Output = new AssetPassPipe { Name = "RobustClientAssetGraphOutput", CheckDuplicates = true };
        NormalizeText = new AssetPassNormalizeText { Name = "RobustClientAssetGraphNormalizeText" };
        MergePrototypeDirectories = new AssetPassMergeTextDirectories(
            "Prototypes",
            "yml",
            // Separate each merged YAML file with a document to provide proper isolation.
            formatterHead: file => $"--- # BEGIN {file}",
            formatterTail: file => $"# END {file}")
        {
            Name = "RobustClientAssetGraphMergePrototypeDirectories"
        };
        MergeLocaleDirectories = new AssetPassMergeTextDirectories(
            "Locale",
            "ftl",
            formatterHead: file => $"# BEGIN {file}",
            formatterTail: file => $"# END {file}")
        {
            Name = "RobustClientAssetGraphMergeLocaleDirectories"
        };
        PackRsis = new AssetPassPackRsis
        {
            Name = "RobustClientAssetGraphPackRsis",
        };

        PresetPasses.AddDependency(Input);
        PackRsis.AddDependency(PresetPasses).AddBefore(NormalizeText);
        MergePrototypeDirectories.AddDependency(PresetPasses).AddBefore(NormalizeText);
        MergeLocaleDirectories.AddDependency(PresetPasses).AddBefore(NormalizeText);
        NormalizeText.AddDependency(PresetPasses).AddBefore(Output);
        // RSI packing goes through text normalization,
        // to catch meta.jsons that have been skipped by the RSI packing pass.
        NormalizeText.AddDependency(PackRsis).AddBefore(Output);
        Output.AddDependency(PresetPasses);
        Output.AddDependency(NormalizeText);
        Output.AddDependency(MergePrototypeDirectories);
        Output.AddDependency(MergeLocaleDirectories);
        Output.AddDependency(PackRsis);

        AllPasses = new AssetPass[]
        {
            Input,
            PresetPasses,
            Output,
            NormalizeText,
            MergePrototypeDirectories,
            MergeLocaleDirectories,
            PackRsis
        };
    }
}
