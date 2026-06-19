using System.Numerics;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Robust.Shared.Maths.Tests
{
    [TestFixture]
    [TestOf(typeof(Box2Rotated))]
    internal sealed class Box2Rotated_Test
    {
        private static IEnumerable<Box2[]> BoxRotations = new[]
        {
            new[]
            {
                new Box2(0.5f, -0.2f, 0.5f, 0.5f),
                new Box2(-0.5f, 0.5f, 0.2f, 0.5f),
                new Box2(-0.5f, -0.5f, -0.5f, 0.2f),
                new Box2(-0.2f, -0.5f, 0.5f, -0.5f)
            },
            new[]
            {
                new Box2(0.5f, 0.5f, 0.5f, 0.5f),
                new Box2(-0.5f, 0.5f, -0.5f, 0.5f),
                new Box2(-0.5f, -0.5f, -0.5f, -0.5f),
                new Box2(0.5f, -0.5f, 0.5f, -0.5f)
            }
        };

        [Test]
        public void FullRotationTest([ValueSource(nameof(BoxRotations))] Box2[] boxes)
        {
            var rotatedBox = new Box2Rotated(boxes[0], Angle.FromDegrees(0));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(rotatedBox.CalcBoundingBox(), Is.Approximately(boxes[i], 0.0001f));
                rotatedBox.Rotation += Angle.FromDegrees(90);
            }

            Assert.That(rotatedBox.CalcBoundingBox(), Is.Approximately(boxes[0], 0.0001f));
        }

        private static readonly float Cos45Deg = MathF.Cos(MathF.PI / 4);
        private static readonly float Sqrt2 = MathF.Sqrt(2);

        public static IEnumerable<(Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected)> CalcBoundingBoxData =>
            new (Box2, Vector2, Angle, Box2)[]
            {
                (new Box2(0, 0, 1, 1), new Vector2(0, 0), 0, new Box2(0, 0, 1, 1)),
                (new Box2(0, 0, 1, 1), new Vector2(0, 0), Math.PI, new Box2(-1, -1, 0, 0)),
                (new Box2(0, 0, 1, 1), new Vector2(1, 0), Math.PI, new Box2(1, -1, 2, 0)),
                (new Box2(0, 0, 1, 1), new Vector2(1, 1), Math.PI, new Box2(1, 1, 2, 2)),
                (new Box2(1, 1, 2, 2), new Vector2(1, 1), Math.PI/4, new Box2(1 - Cos45Deg, 1, 1 + Cos45Deg, 1 + Sqrt2)),
                (new Box2(-1, 1, 1, 2), new Vector2(0, 0), -Math.PI/2, new Box2(1, -1, 2, 1)),
                (Box2.UnitCentered, new Vector2(1, Sqrt2), Angle.FromDegrees(30), new Box2(0.158069f, -0.993544f, 1.52409f,0.372481f)),
            };

        private static TestCaseData[] MatrixBox2Cases =
        [
            new TestCaseData(Matrix3x2.Identity,
                Box2Rotated.UnitCentered,
                Box2Rotated.UnitCentered.CalcBoundingBox()),
            new TestCaseData(Matrix3x2.CreateRotation(MathF.PI),
                Box2Rotated.UnitCentered,
                new Box2Rotated(new Vector2(-0.5f, -0.5f), new Vector2(0.5f, 0.5f)).CalcBoundingBox()),
            new TestCaseData(Matrix3x2.CreateTranslation(Vector2.One),
                Box2Rotated.UnitCentered,
                new Box2Rotated(new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f)).CalcBoundingBox()),
            new TestCaseData(Matrix3x2.CreateTranslation(new Vector2(-1, -Sqrt2))
                             * Matrix3Helpers.CreateTransform(new Vector2(1, Sqrt2), Angle.FromDegrees(30)),
                new Box2Rotated(Box2.UnitCentered),
                new Box2(0.158069f, -0.993544f, 1.52409f, 0.372481f)),
            new TestCaseData(Matrix3x2.CreateTranslation(new Vector2(-1, -Sqrt2))
                             * Matrix3Helpers.CreateTransform(new Vector2(1, Sqrt2), Angle.FromDegrees(30)),
                new Box2Rotated(Box2.UnitCentered, -Angle.FromDegrees(30), new Vector2(1, Sqrt2)),
                Box2.UnitCentered)
        ];

        private static TestCaseData[] GetCornersCases =
        [
            new TestCaseData(new Box2Rotated(new Box2(-1, -2, 3, 4), Angle.FromDegrees(37), new Vector2(1, 2))),
            new TestCaseData(new Box2Rotated(Box2.UnitCentered.Translated(new Vector2(10, 10)), Angle.FromDegrees(45))),
        ];

        private static TestCaseData[] HashCodeIncludesOriginCases =
        [
            new TestCaseData(
                new Box2Rotated(Box2.UnitCentered, Angle.FromDegrees(37), Vector2.Zero),
                new Box2Rotated(Box2.UnitCentered, Angle.FromDegrees(37), Vector2.One)),
            new TestCaseData(
                new Box2Rotated(new Box2(-1, -2, 3, 4), Angle.FromDegrees(90), new Vector2(1, 2)),
                new Box2Rotated(new Box2(-1, -2, 3, 4), Angle.FromDegrees(90), new Vector2(2, 1))),
        ];

        /// <summary>
        /// Tests that transforming a Box2Rotated into a Box2 works.
        /// </summary>
        [Test, TestCaseSource(nameof(MatrixBox2Cases))]
        public void TestBox2Matrices(Matrix3x2 matrix, Box2Rotated bounds, Box2 result)
        {
            Assert.That(matrix.TransformBox(bounds), Is.Approximately(result));
        }

        [Test]
        public void TestCalcBoundingBox([ValueSource(nameof(CalcBoundingBoxData))]
            (Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected) dat)
        {
            var (baseBox, origin, rotation, expected) = dat;

            var rotated = new Box2Rotated(baseBox, rotation, origin);
            Assert.That(rotated.CalcBoundingBox(), Is.Approximately(expected));
        }

        [Test, TestCaseSource(nameof(GetCornersCases))]
        public void TestGetCornersMatchesProperties(Box2Rotated rotated)
        {
            rotated.GetCorners(out var bottomLeft, out var bottomRight, out var topRight, out var topLeft);

            Assert.That(bottomLeft, Is.Approximately(rotated.BottomLeft, 0.0001f));
            Assert.That(bottomRight, Is.Approximately(rotated.BottomRight, 0.0001f));
            Assert.That(topRight, Is.Approximately(rotated.TopRight, 0.0001f));
            Assert.That(topLeft, Is.Approximately(rotated.TopLeft, 0.0001f));
        }

        // Offset it just to make sure the rotation is also gucci.
        private static readonly Vector2 Offset = new Vector2(10.0f, 10.0f);
        private static readonly Angle Rotation = Angle.FromDegrees(45);

        // Box centered at [10, 10] rotated 45 degrees around (0,0) becomes a box centered on the y-axis at y =
        // sqrt(2)*10.
        private static Box2Rotated IntersectionBox = new(Box2.UnitCentered.Translated(Offset), Rotation);
        private static readonly Vector2 IntersectionBoxCenter = Rotation.RotateVec(Offset);

        private static IEnumerable<Vector2> InboundPoints => new Vector2[]
        {
            IntersectionBoxCenter, // center of box
            IntersectionBoxCenter - new Vector2(-0.7f, 0.0f), // lowest point of box (just short of sqrt(0.5) below center)
            IntersectionBoxCenter + new Vector2(0.353f, 0.353f), // close to upper-right flat-edge of box, just shy of 0.5 units from the center
        };

        [Test]
        public void TestPointIntersect([ValueSource(nameof(InboundPoints))] Vector2 point)
        {
            Assert.That(IntersectionBox.Contains(point), $"Rotated box doesn't contain {point}");
        }

        // for the points outside of the box, take the 4 corners that would normally be inside the box if it weren't
        // rotated
        private static IEnumerable<Vector2> OutboundPoints => new Vector2[]
        {
            IntersectionBoxCenter + new Vector2(-0.48f, -0.48f),
            IntersectionBoxCenter + new Vector2(-0.48f, 0.48f),
            IntersectionBoxCenter + new Vector2(0.48f, 0.48f),
            IntersectionBoxCenter + new Vector2(0.48f, -0.48f),
        };

        [Test]
        public void TestPointNoIntersect([ValueSource(nameof(OutboundPoints))] Vector2 point)
        {
            Assert.That(!IntersectionBox.Contains(point), $"Rotated box contains {point}");
        }
    }
}
