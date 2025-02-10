using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed class ClientDynamicParticlesSystem : SharedDynamicParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentGetState>(OnDynamicParticlesComponentGetState);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<DynamicParticlesComponent, ComponentRemove>(HandleComponentRemove);
    }

    private void OnDynamicParticlesComponentGetState(EntityUid uid, DynamicParticlesComponent component, ref ComponentGetState args)
    {
        _particlesManager.DestroyParticleSystem(uid);
        if(_prototypeManager.TryIndex<ParticlesPrototype>("example", out var prototype)){
            ParticleSystemArgs particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            component.particlesSystem = _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }
        else
        {
            throw new InvalidPrototypeNameException($"{component} is not a valid particles prototype");
        }
    }

    private void HandleComponentAdd(EntityUid uid, DynamicParticlesComponent component, ref ComponentAdd args)
    {
        //do a lookup for YAML defined particles
        if(_prototypeManager.TryIndex<ParticlesPrototype>("example", out var prototype)){
            ParticleSystemArgs particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            component.particlesSystem = _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }
        else
        {
            throw new InvalidPrototypeNameException($"{component} is not a valid particles prototype");
        }
    }

    private void HandleComponentRemove(EntityUid uid, DynamicParticlesComponent component, ref ComponentRemove args)
    {
        component.particlesSystem = null;
        _particlesManager.DestroyParticleSystem(uid);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.TryGetModified<EntityPrototype>(out var modified))
            return;
        //TODO reload registered particles
    }
}
