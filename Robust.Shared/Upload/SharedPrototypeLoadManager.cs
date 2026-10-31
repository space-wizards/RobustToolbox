using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public abstract partial class SharedPrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IPrototypeManagerInternal _prototypeManager = default!;
    [Dependency] private ILocalizationManager _localizationManager = default!;
    [Dependency] protected INetManager NetManager = default!;

    [Access(typeof(SharedPrototypeLoadManager))]
    public readonly List<string> LoadedPrototypes = new();

    /// <summary>
    ///     Default size of a single prototype dump part.
    /// </summary>
    public const int DefaultPrototypePartBytes = 256 * 1024;

    /// <summary>
    ///     Upper limit for <see cref="CVars.NetPrototypeDumpPartBytes"/>.
    /// </summary>
    public const int MaxPrototypePartBytes = 4 * 1024 * 1024;

    protected ISawmill Sawmill = default!;

    public virtual void Initialize()
    {
        _replay.RecordingStarted += OnStartReplayRecording;
        Sawmill = Logger.GetSawmill("adminbus");
        NetManager.RegisterNetMessage<GamePrototypeLoadMessage>(LoadPrototypeData);
        NetManager.RegisterNetMessage<GamePrototypeLoadAckMessage>(OnPrototypeLoadAck, NetMessageAccept.Server);
    }

    public abstract void SendGamePrototype(string prototype);

    /// <summary>
    ///     Raised on the server when a connecting client confirms it has applied a prototype dump.
    /// </summary>
    public event Action<INetChannel>? LoadComplete;

    private void OnPrototypeLoadAck(GamePrototypeLoadAckMessage message)
    {
        LoadComplete?.Invoke(message.MsgChannel);
    }

    /// <summary>
    ///     Individual prototypes are never split across parts, and joining the returned parts with "\n\n"
    ///     reproduces the same data as string.Join("\n\n", prototypes)
    /// </summary>
    internal static List<string> SplitPrototypeDump(
        IList<string> prototypes,
        ISawmill? sawmill = null,
        int maxPartBytes = DefaultPrototypePartBytes)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        long partBytes = 0;
        var partStarted = false;

        foreach (var prototype in prototypes)
        {
            var prototypeBytes = Encoding.UTF8.GetByteCount(prototype);

            // +2 accounts for the "\n\n" separator that will be inserted before this prototype.
            if (partStarted && partBytes + 2 + prototypeBytes > maxPartBytes)
            {
                parts.Add(sb.ToString());
                sb.Clear();
                partBytes = 0;
                partStarted = false;
            }

            if (!partStarted && prototypeBytes > maxPartBytes)
            {
                // Splitting inside an element would be corrupted when the client re-joins parts
                // with "\n\n", so an oversized element is sent as a single (larger) part instead.
                sawmill?.Warning(
                    $"A single uploaded prototype is {prototypeBytes} bytes, exceeding the " +
                    $"{maxPartBytes} byte part limit. Sending it as an oversized part.");
            }

            if (partStarted)
            {
                sb.Append("\n\n");
                partBytes += 2;
            }

            sb.Append(prototype);
            partBytes += prototypeBytes;
            partStarted = true;
        }

        if (partStarted)
            parts.Add(sb.ToString());

        return parts;
    }

    protected virtual void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        TryLoadPrototypeData(message.PrototypeData);
    }

    protected bool TryLoadPrototypeData(string data)
    {
        try
        {
            LoadPrototypeData(data);
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to load prototype data. Dropping upload.\n{e}");
            // LoadString can leave partial prototype data behind before failing.
            TryReloadLoadedPrototypeData();
            return false;
        }

        // Add to replay recording after we have loaded the file, in case it contains bad yaml that throws exceptions.
        LoadedPrototypes.Add(data);
        _replay.RecordReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = data });
        Sawmill.Info("Loaded adminbus prototype data.");
        return true;
    }

    private void LoadPrototypeData(string data)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        _prototypeManager.LoadString(data, true, changed);
        if (_cfg.GetCVar(CVars.ResValidatePrototypeUpload))
            _prototypeManager.ReloadPrototypesOrThrow(changed);
        else
            _prototypeManager.ReloadPrototypes(changed);
        _localizationManager.ReloadLocalizations();
    }

    private void TryReloadLoadedPrototypeData()
    {
        try
        {
            _prototypeManager.Reset();
            if (LoadedPrototypes.Count != 0)
                LoadPrototypeData(string.Join("\n\n", LoadedPrototypes));
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to reload accepted prototype data.\n{e}");
        }
    }

    private void OnStartReplayRecording(MappingDataNode metadata, List<object> events)
    {
        foreach (var prototype in LoadedPrototypes)
        {
            events.Add(new ReplayPrototypeUploadMsg { PrototypeData = prototype });
        }
    }
}
