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
    public sealed class ParticlesSystem : SharedParticlesSystem
    {
        [Dependency] private readonly ParticlesManager _particlesManager = default!;
        protected override void OnParticlesComponentGetState(EntityUid uid, SharedParticlesComponent component, ref ComponentGetState args)
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
            particleSystemArgs.Color = (float lifetime) => Color.Red;

            ((ParticlesComponent) component).particlesSystem = _particlesManager.CreateParticleSystem(particleSystemArgs);

        }
    }
}
