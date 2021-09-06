using System.Collections.Generic;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Client.Physics
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPhysicsMapComponent))]
    public sealed class PhysicsMapComponent : SharedPhysicsMapComponent
    {
        private float _timeToSleep;

        protected override void Initialize()
        {
            base.Initialize();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.TimeToSleep, SetTimeToSleep);
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.TimeToSleep, SetTimeToSleep);
        }

        private void SetTimeToSleep(float value) => _timeToSleep = value;

        protected override void Cleanup(float frameTime)
        {
            var toRemove = new List<PhysicsComponent>();

            // Because we're not predicting 99% of bodies its sleep timer never gets incremented so we'll just do it ourselves.
            // (and serializing it over the network isn't necessary?)
            // This is a client-only problem.
            foreach (var body in AwakeBodies)
            {
                if (body.Island || body.LinearVelocity.Length > 0f || body.AngularVelocity != 0f) continue;
                body.SleepTime += frameTime;
                if (body.SleepTime > _timeToSleep)
                {
                    toRemove.Add(body);
                }
            }

            foreach (var body in toRemove)
            {
                body.Awake = false;
            }

            base.Cleanup(frameTime);
        }
    }
}
