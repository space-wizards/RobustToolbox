using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;

namespace Robust.Packaging;

/// <summary>
/// Standard asset graph for packaging server files. Extend by wiring things up to <see cref="InputCore"/>, <see cref="InputResources"/>, and <see cref="Output"/>.
/// </summary>
/// <remarks>
/// <para>
/// This graph has two inputs: one for "core" server files such as the main engine executable, and another for resource files.
/// </para>
/// <para>
/// If you want to add extra passes to run before preset passes, depend them on the relevant input pass, with a before of the relevant preset pass.
/// </para>
/// <para>
/// See the following graph (Mermaid syntax) to understand this asset graph:
/// </para>
/// <code>
/// flowchart LR
///     InputCore              -->    PresetPassesCore
///     PresetPassesCore       --1--> NormalizeTextCore
///     NormalizeTextCore      -->    Output
///     PresetPassesCore       --2--> Output
///     InputResources         -->    PresetPassesResources
///     PresetPassesResources  --1--> AudioMetadata
///     PresetPassesResources  --2--> NormalizeTextResources
///     PresetPassesResources  --3--> PrefixResources
///     AudioMetadata          -->    PrefixResources
///     NormalizeTextResources -->    PrefixResources
///     PrefixResources        -->    Output
/// </code>
/// </remarks>
public sealed class RobustServerAssetGraph
{
    public AssetPassPipe Output { get; }

    /// <summary>
    /// Input pass for core server files, such as <c>Robust.Server.exe</c>.
    /// </summary>
    /// <seealso cref="InputResources"/>
    public AssetPassPipe InputCore { get; }

    public AssetPassPipe PresetPassesCore { get; }

    /// <summary>
    /// Normalizes text files in core files.
    /// </summary>
    public AssetPassNormalizeText NormalizeTextCore { get; }

    /// <summary>
    /// Input pass for server resource files. Everything that will go into <c>Resources/</c>.
    /// </summary>
    /// <remarks>
    /// Do not prefix file paths with <c>Resources/</c>, the asset pass will automatically remap them.
    /// </remarks>
    /// <seealso cref="InputCore"/>
    public AssetPassPipe InputResources { get; }
    public AssetPassPipe PresetPassesResources { get; }
    public AssetPassAudioMetadata AudioMetadata { get; }

    /// <summary>
    /// Normalizes text files in resources.
    /// </summary>
    public AssetPassNormalizeText NormalizeTextResources { get; }

    /// <summary>
    /// Responsible for putting resources into the "<c>Resources/</c>" folder.
    /// </summary>
    public AssetPassPrefix PrefixResources { get; }

    /// <summary>
    /// Collection of all passes in this preset graph.
    /// </summary>
    public IReadOnlyCollection<AssetPass> AllPasses { get; }

    /// <param name="parallel">Should inputs be run in parallel. Should only be turned off for debugging.</param>
    public RobustServerAssetGraph(bool parallel = true)
    {
        Output = new AssetPassPipe { Name = "RobustServerAssetGraphOutput", CheckDuplicates = true };

        //
        // Core files
        //

        // The code injecting the list of source files is assumed to be pretty single-threaded.
        // We use a parallelizing input to break out all the work on files coming in onto multiple threads.
        InputCore = new AssetPassPipe { Name = "RobustServerAssetGraphInputCore", Parallelize = parallel };
        PresetPassesCore = new AssetPassPipe { Name = "RobustServerAssetGraphPresetPassesCore" };
        NormalizeTextCore = new AssetPassNormalizeText { Name = "RobustServerAssetGraphNormalizeTextCore" };

        PresetPassesCore.AddDependency(InputCore);
        NormalizeTextCore.AddDependency(PresetPassesCore).AddBefore(Output);
        Output.AddDependency(PresetPassesCore);
        Output.AddDependency(NormalizeTextCore);

        //
        // Resource files
        //

        // Ditto about parallelizing
        InputResources = new AssetPassPipe { Name = "RobustServerAssetGraphInputResources", Parallelize = parallel };
        PresetPassesResources = new AssetPassPipe { Name = "RobustServerAssetGraphPresetPassesResources" };
        NormalizeTextResources = new AssetPassNormalizeText { Name = "RobustServerAssetGraphNormalizeTextResources" };
        AudioMetadata = new AssetPassAudioMetadata { Name = "RobustServerAssetGraphAudioMetadata" };
        PrefixResources = new AssetPassPrefix("Resources/") { Name = "RobustServerAssetGraphPrefixResources" };

        PresetPassesResources.AddDependency(InputResources);
        AudioMetadata.AddDependency(PresetPassesResources).AddBefore(NormalizeTextResources);
        NormalizeTextResources.AddDependency(PresetPassesResources).AddBefore(PrefixResources);
        PrefixResources.AddDependency(PresetPassesResources);
        PrefixResources.AddDependency(AudioMetadata);
        PrefixResources.AddDependency(NormalizeTextResources);
        Output.AddDependency(PrefixResources);

        AllPasses = new AssetPass[]
        {
            Output,
            InputCore,
            PresetPassesCore,
            NormalizeTextCore,
            InputResources,
            PresetPassesResources,
            NormalizeTextResources,
            AudioMetadata,
            PrefixResources,
        };
    }
}
