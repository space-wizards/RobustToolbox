using Robust.Client.GameStates;
using Robust.Client.Replays.Playback;
using Robust.Client.Serialization;
using Robust.Client.Timing;
using Robust.Client.Upload;
using Robust.Shared;
using Robust.Shared.Network;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager : IReplayLoadManager
{
    [Dependency] private ILogManager _logMan = default!;
    [Dependency] private IBaseClient _client = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private IClientNetManager _netMan = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private ILocalizationManager _locMan = default!;
    [Dependency] private IConfigurationManager _confMan = default!;
    [Dependency] private NetworkResourceManager _netResMan = default!;
    [Dependency] private IClientGameStateManager _gameState = default!;
    [Dependency] private IClientRobustSerializer _serializer = default!;
    [Dependency] private IReplayPlaybackManager _replayPlayback = default!;

    private ushort _metaId;
    private bool _initialized;
    private int _checkpointInterval;
    private int _checkpointEntitySpawnThreshold;
    private int _checkpointEntityStateThreshold;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _confMan.OnValueChanged(CVars.CheckpointInterval, value => _checkpointInterval = value, true);
        _confMan.OnValueChanged(CVars.CheckpointEntitySpawnThreshold, value => _checkpointEntitySpawnThreshold = value,
            true);
        _confMan.OnValueChanged(CVars.CheckpointEntityStateThreshold, value => _checkpointEntityStateThreshold = value,
            true);
        _metaId = _factory.GetRegistration(typeof(MetaDataComponent)).NetID!.Value;
        _sawmill = _logMan.GetSawmill("replay");
    }
}
