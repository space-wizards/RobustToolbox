using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed class ClientYamlParticlesSystem : ServerYamlParticlesSystem
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    public override void Initialize() {
        base.Initialize();
        //SubscribeLocalEvent<YamlParticlesComponent, ComponentGetState>(OnYamlParticlesComponentGetState);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<YamlParticlesComponent, ComponentAdd>(HandleComponentAdd);
        SubscribeLocalEvent<YamlParticlesComponent, ComponentRemove>(HandleComponentRemove);
    }


    private void HandleComponentAdd(EntityUid uid, YamlParticlesComponent component, ref ComponentAdd args)
    {
        ParticleSystemArgs particleSystemArgs;
        component.ParticleType = "example";
        if(!string.IsNullOrEmpty(component.ParticleType)){
            //do a lookup for YAML defined particles
            ParticlesPrototype prototype = _prototypeManager.Index<ParticlesPrototype>(component.ParticleType);
            particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            component.particlesSystem = _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }
        else
        {
            throw new InvalidPrototypeNameException($"{component.ParticleType} is not a valid particles prototype");
        }


    }

    private void HandleComponentRemove(EntityUid uid, YamlParticlesComponent component, ref ComponentRemove args)
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
