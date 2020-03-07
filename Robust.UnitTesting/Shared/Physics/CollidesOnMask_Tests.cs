using Moq;
using NUnit.Framework;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    internal class CollidesOnMask_Tests
    {
        private Mock<IPhysBody> A;
        private Mock<IPhysBody> B;

        private bool Result;

        private void SetupDefault()
        {
            A = new Mock<IPhysBody>();
            A.Setup(x => x.CollisionEnabled).Returns(true);

            B = new Mock<IPhysBody>();
            B.Setup(x => x.CollisionEnabled).Returns(true);
        }

        private void Act()
        {
            Result = PhysicsManager.CollidesOnMask(A.Object, B.Object);
        }

        [TestCase(0, 0, 0, 0, false)]
        [TestCase(0, 0, 0, 1, false)]
        [TestCase(0, 0, 1, 0, false)]
        [TestCase(0, 0, 1, 1, false)]
        [TestCase(0, 1, 0, 0, false)]
        [TestCase(0, 1, 0, 1, false)]
        [TestCase(0, 1, 1, 0, true)]
        [TestCase(0, 1, 1, 1, true)]
        [TestCase(1, 0, 0, 0, false)]
        [TestCase(1, 0, 0, 1, true)]
        [TestCase(1, 0, 1, 0, false)]
        [TestCase(1, 0, 1, 1, true)]
        [TestCase(1, 1, 0, 0, false)]
        [TestCase(1, 1, 0, 1, true)]
        [TestCase(1, 1, 1, 0, true)]
        [TestCase(1, 1, 1, 1, true)]
        public void GivenMasksAndLayers_WhenCollidesOnMask_ThenExpected(int layerA, int maskA, int layerB, int maskB, bool expected)
        {
            //Arrange
            SetupDefault();
            A.Setup(x => x.CollisionLayer).Returns(layerA);
            A.Setup(x => x.CollisionMask).Returns(maskA);
            B.Setup(x => x.CollisionLayer).Returns(layerB);
            B.Setup(x => x.CollisionMask).Returns(maskB);
            //Act
            Act();
            //Assert
            Assert.AreEqual(expected, Result);
        }

        [Test]
        public void GivenLockerAndItem_WhenCollidesOnMask_ThenFalse()
        {
            var layerA = 31;
            var maskA = 30;
            var layerB = 32;
            var maskB = 0;
            GivenMasksAndLayers_WhenCollidesOnMask_ThenExpected(layerA, maskA, layerB, maskB, false);
        }
    }
}
