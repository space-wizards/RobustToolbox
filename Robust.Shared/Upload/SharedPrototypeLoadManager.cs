using System;
using System.Collections.Generic;
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
public abstract class SharedPrototypeLoadManager : IGamePrototypeLoadManager
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] protected readonly INetManager NetManager = default!;

    [Access(typeof(SharedPrototypeLoadManager))]
    public readonly List<string> LoadedPrototypes = new();

    private ISawmill _sawmill = default!;

    public virtual void Initialize()
    {
        _replay.RecordingStarted += OnStartReplayRecording;
        _sawmill = Logger.GetSawmill("adminbus");
        NetManager.RegisterNetMessage<GamePrototypeLoadMessage>(LoadPrototypeData);
    }

    public abstract void SendGamePrototype(string prototype);

    protected virtual void LoadPrototypeData(GamePrototypeLoadMessage message)
    {
        var data = message.PrototypeData;

        // TODO validate yaml before loading?

        var changed = new Dictionary<Type, HashSet<string>>();
        _prototypeManager.LoadString(data, true, changed);
        _prototypeManager.ReloadPrototypes(changed);
        _localizationManager.ReloadLocalizations();

        // Add to replay recording after we have loaded the file, in case it contains bad yaml that throws exceptions.
        LoadedPrototypes.Add(data);
        _replay.RecordReplayMessage(new ReplayPrototypeUploadMsg { PrototypeData = data });
        _sawmill.Info("Loaded adminbus prototype data.");
    }

    private void OnStartReplayRecording(MappingDataNode metadata, List<object> events)
    {
        foreach (var prototype in LoadedPrototypes)
        {
            events.Add(new ReplayPrototypeUploadMsg { PrototypeData = prototype });
        }
    }
}
