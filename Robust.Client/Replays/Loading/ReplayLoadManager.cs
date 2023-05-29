using Robust.Client.GameStates;
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

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager : IReplayLoadManager
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IClientNetManager _netMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ILocalizationManager _locMan = default!;
    [Dependency] private readonly IConfigurationManager _confMan = default!;
    [Dependency] private readonly NetworkResourceManager _netResMan = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientRobustSerializer _serializer = default!;

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
        _confMan.OnValueChanged(CVars.CheckpointEntitySpawnThreshold, value => _checkpointEntitySpawnThreshold = value, true);
        _confMan.OnValueChanged(CVars.CheckpointEntityStateThreshold, value => _checkpointEntityStateThreshold = value, true);
        _metaId = _factory.GetRegistration(typeof(MetaDataComponent)).NetID!.Value;
        _sawmill = Logger.GetSawmill("replay");
    }
}
