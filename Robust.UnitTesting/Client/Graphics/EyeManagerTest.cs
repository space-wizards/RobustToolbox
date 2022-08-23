using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.Graphics
{
    [TestFixture]
    [TestOf(typeof(IEyeManager))]
    public sealed class EyeManagerTest : RobustIntegrationTest
    {
        [Test]
        public async Task TestViewportRotation()
        {
            // At cardinal rotations with a square viewport these should all be the same.
            var client = StartClient();
            await client.WaitIdleAsync();

            var eyeManager = client.ResolveDependency<IEyeManager>();

            await client.WaitAssertion(() =>
            {
                // At this stage integration tests aren't pooled so no way I'm making each of these a new test for now.
                foreach (var angle in new[]
                {
                    Angle.Zero,
                    new Angle(Math.PI / 4),
                    new Angle(Math.PI / 2),
                    new Angle(Math.PI),
                    new Angle(-Math.PI / 4),
                    new Angle(-Math.PI / 2),
                    new Angle(-Math.PI)
                })
                {
                    var worldAABB = eyeManager.GetWorldViewport();
                    var worldPort = eyeManager.GetWorldViewbounds();

                    Assert.That(worldAABB.EqualsApprox(worldPort.CalcBoundingBox()), $"Invalid EyeRotation bounds found for {angle}: Expected {worldAABB} and received {worldPort.CalcBoundingBox()}");
                }
            });
        }
    }
}
