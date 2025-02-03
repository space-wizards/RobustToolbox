using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects;

public abstract class SharedParticlesSystem : EntitySystem {

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<SharedParticlesComponent, ComponentGetState>(OnParticlesComponentGetState);
    }

    protected abstract void OnParticlesComponentGetState(EntityUid uid, SharedParticlesComponent component, ref ComponentGetState args);
}
