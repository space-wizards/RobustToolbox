using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;
using System;
using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Replays;

public interface IReplayRecordingManager
{
    /// <summary>
    ///     Queues some net-serializable data to be saved for replaying
    /// </summary>
    /// <remarks>
    ///     The queued object is typically something like an <see cref="EntityEventArgs"/>, so that replays can
    ///     simulate receiving networked messages. However, this can really be any serializable data and could be used
    ///     for saving server-exclusive data like power net or atmos pipe-net data for replaying. Alternatively, those
    ///     could also just use networked component states on entities that are in null space and hidden from all
    ///     players (but still saved to replays).
    /// </remarks>
    void QueueReplayMessage(object args);

    /// <summary>
    ///     Whether the server is currently recording replay data.
    /// </summary>
    bool Recording { get; }

    /// <summary>
    ///     This gets invoked whenever a replay recording starts. Subscribers can use this to add extra yaml metadata
    ///     data to the recording, as well as to effectively "raise" networked events that would get sent to a newly
    ///     connecting "client".
    /// </summary>
    event Action<(MappingDataNode, List<object>)>? OnRecordingStarted;

    /// <summary>
    ///     This gets invoked whenever a replay recording ends. Subscribers can use this to add extra yaml metadata data to the recording.
    /// </summary>
    event Action<MappingDataNode>? OnRecordingStopped;


    // Define misc constants both for writing and reading replays.
    # region Constants

    /// <summary>
    ///     File extension for data files that have to be deserialized and decompressed.
    /// </summary>
    public const string Ext = "dat";

    // filenames
    public static readonly ResPath MetaFile = new($"replay.yml");
    public static readonly ResPath CvarFile = new($"cvars.toml");
    public static readonly ResPath StringsFile = new($"strings.{Ext}");
    public static readonly ResPath InitFile = new($"init.{Ext}");

    // Yaml keys
    public const string Hash = "typeHash";
    public const string CompHash = "componentHash";
    public const string Strings = "stringHash";
    public const string Time = "time";
    public const string Name = "name";
    public const string Tick = "serverStartTime";
    public const string ServerTime = "startTick";
    public const string BaseTick = "timeBaseTick";
    public const string BaseTime = "timeBaseTimespan";
    public const string Duration = "duration";
    public const string Engine = "engineVersion";
    public const string Fork = "buildForkId";
    public const string ForkVersion = "buildVersion";
    public const string FileCount = "fileCount";
    public const string Compressed = "size";
    public const string Uncompressed = "uncompressedSize";
    public const string EndTick = "endTick";
    public const string EndTime = "serverEndTime";

    #endregion
}
