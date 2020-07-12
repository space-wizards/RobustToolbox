using NUnit.Framework;
using Robust.Client.Graphics.ClientEye;
using Robust.Shared.Map;
using Robust.Shared.Maths;

// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Robust.UnitTesting.Client.Graphics
{
    [TestFixture, Parallelizable, TestOf(typeof(Eye))]
    class EyeTest
    {
        [Test]
        public void Constructor_DefaultZoom_isOne()
        {
            var eye = new Eye();

            Assert.That(eye.Zoom, Is.Approximately(Vector2.One));
        }

        [Test]
        public void Constructor_DefaultPosition_isZero()
        {
            var eye = new Eye();

            Assert.That(eye.Position, Is.EqualTo(MapCoordinates.Nullspace));
        }

        [Test]
        public void Constructor_DefaultDrawFov_isTrue()
        {
            var eye = new Eye();

            Assert.That(eye.DrawFov, Is.True);
        }
    }
}
