using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Replays;

/// <summary>
/// Contains various constants related to the replay recording subsystem.
/// </summary>
public static class ReplayConstants
{
    /// <summary>
    /// File extension for data files that have to be deserialized and decompressed.
    /// </summary>
    public const string Ext = "dat";

    /// <summary>
    /// Prefix used by serialized data messages.
    /// </summary>
    public const string DataFilePrefix = "data_";

    // file names

    /// <summary>
    /// File that contains primary replay metadata.
    /// </summary>
    public static readonly ResPath FileMeta = new($"replay.yml");

    /// <summary>
    /// File that contains final replay metadata written at the end of a successful recording.
    /// </summary>
    public static readonly ResPath FileMetaFinal = new($"replay_final.yml");

    /// <summary>
    /// File that contains CVars at the start of a recording.
    /// </summary>
    public static readonly ResPath FileCvars = new($"cvars.toml");

    /// <summary>
    /// File that contains the serialization string map (<see cref="IRobustMappedStringSerializer"/>).
    /// </summary>
    public static readonly ResPath FileStrings = new($"strings.{Ext}");

    /// <summary>
    /// File that contains extra initialization objects provided by content.
    /// </summary>
    public static readonly ResPath FileInit = new($"init.{Ext}");

    /// <summary>
    /// Folder inside replay zip files that replay data is contained in.
    /// </summary>
    public static readonly ResPath ReplayZipFolder = new("_replay");

    // Keys for the YAML data in replay.yml

    /// <summary>
    /// Type hash from <see cref="IRobustSerializer"/>.
    /// </summary>
    public const string MetaKeyTypeHash = "typeHash";

    /// <summary>
    /// Component hash from <see cref="IComponentFactory"/>.
    /// </summary>
    public const string MetaKeyComponentHash = "componentHash";

    /// <summary>
    /// String hash from <see cref="IRobustMappedStringSerializer"/>.
    /// </summary>
    public const string MetaKeyStringHash = "stringHash";

    /// <summary>
    /// Time the recording was started, in UTC.
    /// </summary>
    public const string MetaKeyTime = "time";

    /// <summary>
    /// The name of the recording.
    /// </summary>
    public const string MetaKeyName = "name";

    /// <summary>
    /// The tick the recording was started at.
    /// </summary>
    public const string MetaKeyStartTick = "startTick";

    /// <summary>
    /// The server time the recording was started at.
    /// </summary>
    public const string MetaKeyStartTime = "startTime";

    /// <summary>
    /// The base tick from <see cref="IGameTiming"/> when the recording was started.
    /// </summary>
    public const string MetaKeyBaseTick = "timeBaseTick";

    /// <summary>
    /// The base time from <see cref="IGameTiming"/> when the recording was started.
    /// </summary>
    public const string MetaKeyBaseTime = "timeBaseTime";

    /// <summary>
    /// The engine version that was recorded on.
    /// </summary>
    public const string MetaKeyEngineVersion = "engineVersion";

    /// <summary>
    /// The build fork ID that was recorded on.
    /// </summary>
    public const string MetaKeyForkId = "buildForkId";

    /// <summary>
    /// The build fork version that was recorded on.
    /// </summary>
    public const string MetaKeyForkVersion = "buildForkVersion";

    /// <summary>
    /// Is this a client-side recording?
    /// </summary>
    public const string MetaKeyIsClientRecording = "isClientRecording";

    /// <summary>
    /// If this is a client recording, what is the User ID player.
    /// </summary>
    public const string MetaKeyRecordedBy = "recordedBy";

    // Keys for the YAML data in replay_final.yml

    /// <summary>
    /// How many individual data files have been recorded in total.
    /// </summary>
    public const string MetaFinalKeyFileCount = "fileCount";

    /// <summary>
    /// Length of the recording.
    /// </summary>
    public const string MetaFinalKeyDuration = "duration";

    /// <summary>
    /// Compressed total size of the replay data files.
    /// </summary>
    public const string MetaFinalKeyCompressedSize = "size";

    /// <summary>
    /// Uncompressed total size of the replay data files.
    /// </summary>
    public const string MetaFinalKeyUncompressedSize = "uncompressedSize";

    /// <summary>
    /// Tick the recording ends at.
    /// </summary>
    public const string MetaFinalKeyEndTick = "endTick";

    /// <summary>
    /// Time the recording ends at.
    /// </summary>
    public const string MetaFinalKeyEndTime = "endTime";
}
