using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Server.GameObjects;

public sealed class ParticlesSystem : SharedParticlesSystem
{
    protected override void OnParticlesComponentGetState(EntityUid uid, ParticlesComponent component, ref ComponentGetState args)
    {

    }
}
