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
///     Manages runtime-loaded prototype uploads and their distribution.
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

    protected long LoadedPrototypesSize { get; private set; } = PrototypeDumpFormat.HeaderSize;

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
        return _cfg.GetCVar(CVars.NetPrototypeDumpMaxBytes);
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
        AddLoadedPrototype(data);
        _replay.RecordReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = data });
        Sawmill.Info("Loaded adminbus prototype data.");
        return true;
    }

    protected bool TryApplyPrototypeDump(IReadOnlyList<string> prototypes, bool sequential)
    {
        if (prototypes.Count == 0)
            return true;

        try
        {
            if (sequential)
                ApplyPrototypeUploads(prototypes);
            else
                ApplyPrototypeDumpBatch(prototypes);
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to apply prototype dump. Dropping it.\n{e}");
            TryReloadLoadedPrototypeData();
            return false;
        }

        foreach (var data in prototypes)
        {
            AddLoadedPrototype(data);
            _replay.RecordReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = data });
        }

        Sawmill.Info("Loaded adminbus prototype dump.");
        return true;
    }

    private void AddLoadedPrototype(string data)
    {
        LoadedPrototypes.Add(data);
        LoadedPrototypesSize += PrototypeDumpFormat.GetPayloadSize(0, data);
    }

    private void ApplyPrototypeDumpBatch(IReadOnlyList<string> prototypes)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        var seen = new HashSet<(Type Kind, string Id)>();

        foreach (var data in prototypes)
        {
            var uploadChanged = new Dictionary<Type, HashSet<string>>();
            LoadPrototypeDataInto(data, uploadChanged);

            if (MergeChanged(changed, uploadChanged, seen))
            {
                // Duplicate id: apply uploads one by one.
                ReloadLoadedPrototypeData();
                ApplyPrototypeUploads(prototypes);
                return;
            }
        }

        ReloadPrototypes(changed);
    }

    private void ApplyPrototypeUploads(IReadOnlyList<string> prototypes)
    {
        var changed = new Dictionary<Type, HashSet<string>>();

        for (var i = 0; i < prototypes.Count; i++)
        {
            var uploadChanged = new Dictionary<Type, HashSet<string>>();
            LoadPrototypeDataInto(prototypes[i], uploadChanged);

            if (i < prototypes.Count - 1)
                _prototypeManager.ReloadPrototypes(uploadChanged);

            MergeChanged(changed, uploadChanged);
        }

        ReloadPrototypes(changed);
    }

    private static bool MergeChanged(
        Dictionary<Type, HashSet<string>> changed,
        Dictionary<Type, HashSet<string>> uploadChanged,
        HashSet<(Type Kind, string Id)>? seen = null)
    {
        var duplicate = false;

        foreach (var (kind, ids) in uploadChanged)
        {
            if (!changed.TryGetValue(kind, out var changedIds))
                changed[kind] = changedIds = new HashSet<string>();

            foreach (var id in ids)
            {
                changedIds.Add(id);

                if (seen != null && !seen.Add((kind, id)))
                    duplicate = true;
            }
        }

        return duplicate;
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
            ReloadLoadedPrototypeData();
        }
        catch (Exception e)
        {
            Sawmill.Error($"Failed to reload accepted prototype data.\n{e}");
        }
    }

    private void ReloadLoadedPrototypeData()
    {
        _prototypeManager.Reset();
        if (LoadedPrototypes.Count == 0)
            return;

        ApplyPrototypeUploads(LoadedPrototypes);
    }

    private void OnStartReplayRecording(MappingDataNode metadata, List<object> events)
    {
        foreach (var prototype in LoadedPrototypes)
        {
            events.Add(new ReplayPrototypeUploadMsg { PrototypeData = prototype });
        }
    }
}
