using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
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


        public override void Initialize() {
            base.Initialize();
            //SubscribeLocalEvent<ParticlesComponent, ComponentGetState>(OnParticlesComponentGetState);
            SubscribeLocalEvent<ParticlesComponent, ComponentAdd>(HandleComponentAdd);
            SubscribeLocalEvent<ParticlesComponent, ComponentRemove>(HandleComponentRemove);
        }


        private void HandleComponentAdd(EntityUid uid, ParticlesComponent component, ref ComponentAdd args)
        {
            //do a lookup for some yaml thing or some such based on particle type
            ParticleSystemArgs particleSystemArgs = new(
                () => Texture.White,
                new Vector2(200,200),
                100,
                10.0f
                );
            particleSystemArgs.Acceleration = (float lifetime) => new Vector3(lifetime);
            particleSystemArgs.SpawnPosition = () => new Vector3(new Random().NextFloat()*200, 0, 0);
            particleSystemArgs.Color = (float lifetime) => Color.FromArgb(255,255,(byte)((lifetime/3.0)*255),0);
            particleSystemArgs.ParticleCount=1000;

            component.particlesSystem = _particlesManager.CreateParticleSystem(uid, particleSystemArgs);

        }

        private void HandleComponentRemove(EntityUid uid, ParticlesComponent component, ref ComponentRemove args)
        {
            component.particlesSystem = null;
            _particlesManager.DestroyParticleSystem(uid);
        }
    }
}
