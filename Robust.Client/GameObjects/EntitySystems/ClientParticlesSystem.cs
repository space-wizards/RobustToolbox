using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System;
using System.Drawing;
using System.Numerics;


namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class ClientParticlesSystem : SharedParticlesSystem
    {
        [Dependency] private readonly ParticlesManager _particlesManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


        public override void Initialize() {
            base.Initialize();
            //SubscribeLocalEvent<ParticlesComponent, ComponentGetState>(OnParticlesComponentGetState);
            SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
            SubscribeLocalEvent<ParticlesComponent, ComponentAdd>(HandleComponentAdd);
            SubscribeLocalEvent<ParticlesComponent, ComponentRemove>(HandleComponentRemove);
        }


        private void HandleComponentAdd(EntityUid uid, ParticlesComponent component, ref ComponentAdd args)
        {
            ParticleSystemArgs particleSystemArgs;
            component.ParticleType = "example";
            if(!string.IsNullOrEmpty(component.ParticleType)){
                //do a lookup for YAML defined particles
                ParticlesPrototype prototype = _prototypeManager.Index<ParticlesPrototype>(component.ParticleType);
                particleSystemArgs = prototype.GetParticleSystemArgs(_resourceCache);
            }
            else
            {
                particleSystemArgs = new(
                    () => _resourceCache.GetResource<TextureResource>("/OpenDream/Logo/logo.png"),
                    new Shared.Maths.Vector2i(200,200),
                    100,
                    10.0f
                );
                particleSystemArgs.Acceleration = (float lifetime) => new Robust.Shared.Maths.Vector3(0f,-1f,0f);
                particleSystemArgs.SpawnPosition = () => new Robust.Shared.Maths.Vector3(100, 0, 0);
                particleSystemArgs.SpawnVelocity = () => new Robust.Shared.Maths.Vector3((new Random().NextFloat()*500)-250,new Random().NextFloat()*205,0);
                particleSystemArgs.Color = (float lifetime) => Color.FromArgb(255,255,255,(byte)((lifetime/3.0)*255));
                particleSystemArgs.Transform = (float lifetime) => new Matrix3x2(0.25f,0,
                                                                                0,0.25f,
                                                                                40,80);
                particleSystemArgs.ParticleCount=100;
            }
            component.particlesSystem = _particlesManager.CreateParticleSystem(uid, particleSystemArgs);
        }

        private void HandleComponentRemove(EntityUid uid, ParticlesComponent component, ref ComponentRemove args)
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
}
