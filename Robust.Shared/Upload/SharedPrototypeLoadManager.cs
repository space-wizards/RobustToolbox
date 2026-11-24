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
        TransferManager.RegisterTransferMessage(
            TransferKeyPrototypeDump,
            OnPrototypeDumpReceived,
            NetMessageAccept.Client);
    }

    public abstract void SendGamePrototype(string prototype);

    /// <summary>
    ///     Called on the client when a prototype dump transfer has been received.
    /// </summary>
    protected virtual void OnPrototypeDumpReceived(TransferReceivedEvent ev) { }

    protected virtual void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        TryLoadPrototypeData(message.PrototypeData);
    }

    protected virtual bool CanAcceptPrototype(string data)
    {
        return true;
    }

    protected int GetPrototypeDumpMaxBytes()
    {
        return Math.Max(0, _cfg.GetCVar(CVars.NetPrototypeDumpMaxBytes));
    }

    protected bool TryLoadPrototypeData(string data)
    {
        if (!CanAcceptPrototype(data))
            return false;

        try
        {
            ApplyPrototypeData(data);
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

    // Client-side dump apply. Server uploads go through TryLoadPrototypeData.
    protected bool TryApplyPrototypeDump(IReadOnlyList<string> prototypes)
    {
        if (prototypes.Count == 0)
            return true;

        var changed = new Dictionary<Type, HashSet<string>>();

        try
        {
            foreach (var data in prototypes)
                LoadPrototypeDataInto(data, changed);

            ReloadPrototypes(changed);
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to apply prototype dump. Dropping it.\n{e}");
            TryReloadLoadedPrototypeData();
            return false;
        }

        foreach (var data in prototypes)
        {
            LoadedPrototypes.Add(data);
            _replay.RecordReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = data });
        }

        Sawmill.Info("Loaded adminbus prototype dump.");
        return true;
    }

    private void LoadPrototypeDataInto(string data, Dictionary<Type, HashSet<string>> changed)
    {
        _prototypeManager.LoadString(data, true, changed);
    }

    private void ReloadPrototypes(Dictionary<Type, HashSet<string>> changed)
    {
        if (_cfg.GetCVar(CVars.ResValidatePrototypeUpload))
            _prototypeManager.ReloadPrototypesOrThrow(changed);
        else
            _prototypeManager.ReloadPrototypes(changed);

        _localizationManager.ReloadLocalizations();
    }

    private void ApplyPrototypeData(string data)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        LoadPrototypeDataInto(data, changed);
        ReloadPrototypes(changed);
    }

    private void TryReloadLoadedPrototypeData()
    {
        try
        {
            _prototypeManager.Reset();
            if (LoadedPrototypes.Count == 0)
                return;

            var changed = new Dictionary<Type, HashSet<string>>();
            foreach (var prototype in LoadedPrototypes)
                LoadPrototypeDataInto(prototype, changed);

            ReloadPrototypes(changed);
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
