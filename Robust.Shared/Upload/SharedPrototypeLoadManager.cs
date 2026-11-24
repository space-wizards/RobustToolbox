using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Upload;

/// <summary>
///     Manages sending runtime-loaded prototypes from game staff to clients.
/// </summary>
public abstract partial class SharedPrototypeLoadManager : IGamePrototypeLoadManager
{
    /// <summary>
    ///     Transfer key used to send the prototype dump to connecting clients.
    /// </summary>
    internal const string TransferKeyPrototypeDump = "TransferKeyPrototypeDump";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IPrototypeManagerInternal _prototypeManager = default!;
    [Dependency] private ILocalizationManager _localizationManager = default!;
    [Dependency] protected INetManager NetManager = default!;
    [Dependency] protected ITransferManager TransferManager = default!;

    [Access(typeof(SharedPrototypeLoadManager))]
    public readonly List<string> LoadedPrototypes = new();

    protected ISawmill Sawmill = default!;

    public virtual void Initialize()
    {
        _replay.RecordingStarted += OnStartReplayRecording;
        Sawmill = Logger.GetSawmill("adminbus");
        NetManager.RegisterNetMessage<GamePrototypeLoadMessage>(LoadPrototypeData);
        NetManager.RegisterNetMessage<GamePrototypeLoadAckMessage>(OnPrototypeLoadAck, NetMessageAccept.Server);
        TransferManager.RegisterTransferMessage(
            TransferKeyPrototypeDump,
            OnPrototypeDumpReceived,
            NetMessageAccept.Client);
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
    ///     Called on the client when a prototype dump transfer has been received.
    /// </summary>
    protected virtual void OnPrototypeDumpReceived(TransferReceivedEvent ev)
    {
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
