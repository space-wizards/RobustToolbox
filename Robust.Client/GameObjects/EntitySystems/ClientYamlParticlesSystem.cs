using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed class ClientYamlParticlesSystem : SharedYamlParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<YamlParticlesComponent, AfterAutoHandleStateEvent>(OnYamlParticlesComponentChange);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<YamlParticlesComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<YamlParticlesComponent, ComponentRemove>(HandleComponentRemove);
    }

    private void OnYamlParticlesComponentChange(EntityUid uid, YamlParticlesComponent component, ref AfterAutoHandleStateEvent args)
    {
        if(_prototypeManager.TryIndex<ParticlesPrototype>(component.ParticleType, out var prototype)){
            ParticleSystemArgs particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            if(_particlesManager.TryGetParticleSystem(uid, out var system))
                system.UpdateSystem(particleSystemArgs);
            else
                _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }
        else
        {
            throw new InvalidPrototypeNameException($"{component.ParticleType} is not a valid particles prototype");
        }
    }

    private void HandleComponentAdd(EntityUid uid, YamlParticlesComponent component, ref ComponentAdd args)
    {
        if(string.IsNullOrEmpty(component.ParticleType))
            return;
        //do a lookup for YAML defined particles
        if(_prototypeManager.TryIndex<ParticlesPrototype>(component.ParticleType, out var prototype)){
            ParticleSystemArgs particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }
        else
        {
            throw new InvalidPrototypeNameException($"{component.ParticleType} is not a valid particles prototype");
        }
    }

    private void HandleComponentRemove(EntityUid uid, YamlParticlesComponent component, ref ComponentRemove args)
    {
        _particlesManager.DestroyParticleSystem(uid);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.TryGetModified<EntityPrototype>(out var modified))
            return;
        //TODO reload registered particles
    }
}
