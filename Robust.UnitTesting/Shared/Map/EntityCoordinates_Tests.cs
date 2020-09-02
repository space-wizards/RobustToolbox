using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Is = Robust.UnitTesting.Is;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityCoordinates))]
    public class EntityCoordinates_Tests
    {
        /// <summary>
        /// Passing an invalid entity ID into the constructor makes the coordinates invalid.
        /// </summary>
        [Test]
        public void IsValid_InvalidEntId_False()
        {
            // Arrange
            var mockEntityMan = new Mock<IEntityManager>();
            var coords = new EntityCoordinates(EntityUid.Invalid, Vector2.Zero);

            // Act
            var result = coords.IsValid(mockEntityMan.Object);

            // Assert
            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Passing an valid but nonexistent entity ID into the constructor makes the coordinates invalid.
        /// </summary>
        [Test]
        public void IsValid_EntityDeleted_False()
        {
            // Arrange
            var mockEntityMan = new Mock<IEntityManager>();
            var coords = new EntityCoordinates(new EntityUid(1), Vector2.Zero);

            // Act
            var result = coords.IsValid(mockEntityMan.Object);

            // Assert
            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Passing an valid but nonexistent entity ID into the constructor makes the coordinates invalid.
        /// </summary>
        [TestCase(float.NaN, float.NaN)]
        [TestCase(0, float.NaN)]
        [TestCase(float.NaN, 0)]
        public void IsValid_NonFiniteVector_False(float x, float y)
        {
            // Arrange
            var mockEntityMan = new Mock<IEntityManager>();
            mockEntityMan.Setup(m => m.EntityExists(It.IsAny<EntityUid>())).Returns(true);
            var coords = new EntityCoordinates(new EntityUid(1), new Vector2(x, y));

            // Act
            var result = coords.IsValid(mockEntityMan.Object);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}
