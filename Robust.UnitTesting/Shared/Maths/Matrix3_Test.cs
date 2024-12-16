using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestOf(typeof(Matrix3x2))]
    public sealed class Matrix3_Test
    {
        private static readonly TestCaseData[] Rotations = new TestCaseData[]
        {
            new(Matrix3x2.Identity, Angle.Zero),
            new(Matrix3x2.CreateRotation(MathF.PI / 2f), new Angle(Math.PI / 2)),
            new(Matrix3x2.CreateRotation(MathF.PI), new Angle(Math.PI)),
        };

        [Test, TestCaseSource(nameof(Rotations))]
        public void GetRotationTest(Matrix3x2 matrix, Angle angle)
        {
            Assert.That(angle, Is.EqualTo(matrix.Rotation()));
        }

        [Test]
        public void TranslationTest()
        {
            var control = new Vector2(1, 1);
            var matrix = Matrix3Helpers.CreateTranslation(control);

            var origin = new Vector2(0, 0);
            var result = Vector2.Transform(origin, matrix);

            Assert.That(control, Is.EqualTo(result), result.ToString);
        }

        private static readonly IEnumerable<(Vector2, double)> _rotationTests = new[]
        {
            (new Vector2( 1, 0).Normalized(), 0.0),
            (new Vector2( 1, 1).Normalized(), 1 * System.Math.PI / 4.0),
            (new Vector2( 0, 1).Normalized(), 1 * System.Math.PI / 2.0),
            (new Vector2(-1, 1).Normalized(), 3 * System.Math.PI / 4.0),
            (new Vector2(-1, 0).Normalized(), 1 * System.Math.PI / 1.0),
            (new Vector2(-1,-1).Normalized(), 5 * System.Math.PI / 4.0),
            (new Vector2( 0,-1).Normalized(), 3 * System.Math.PI / 2.0),
            (new Vector2( 1,-1).Normalized(), 7 * System.Math.PI / 4.0),
        };

        [Test]
        [Sequential]
        public void RotationTest([ValueSource(nameof(_rotationTests))] (Vector2, double) testCase)
        {
            var angle = testCase.Item2;

            var matrix = Matrix3Helpers.CreateRotation((float)angle);

            var test = new Vector2(1, 0);
            var result = Vector2.Transform(test, matrix);

            var control = testCase.Item1;

            Assert.That(MathHelper.CloseToPercent(control.X, result.X), Is.True, result.ToString);
            Assert.That(MathHelper.CloseToPercent(control.Y, result.Y), Is.True, result.ToString);
        }

        [Test]
        public void MultiplyTransformOrder()
        {
            var startPoint = new Vector2(1, 0);

            Vector2 scale = new Vector2(2, 2);
            Angle angle = new Angle(System.MathF.PI / 2);
            Vector2 offset = new Vector2(-5, -3);

            var scaleMatrix = Matrix3Helpers.CreateScale(scale);
            var rotateMatrix = Matrix3Helpers.CreateRotation(angle);
            var translateMatrix = Matrix3Helpers.CreateTranslation(offset);

            // 1. Take the start point  -> ( 1, 0)
            // 2. Scale it by 2         -> ( 2, 0)
            // 3. Rotate by +90 degrees -> ( 0, 2)
            // 4. Translate by (-5, -3) -> (-5,-1)
            var result = Vector2.Transform(startPoint, scaleMatrix * rotateMatrix * translateMatrix);

            Assert.That(result.X, Is.Approximately(-5f));
            Assert.That(result.Y, Is.Approximately(-1f));

            // repeat but with CreateTransform()
            var transform = Matrix3Helpers.CreateTransform(offset, angle, scale);
            result = Vector2.Transform(startPoint, transform);
            Assert.That(result.X, Is.Approximately(-5f));
            Assert.That(result.Y, Is.Approximately(-1f));
        }

        [Test]
        public void InverseTransformTest()
        {
            Vector2 scale = new Vector2(2.32f, 2);
            Angle angle = new Angle(System.MathF.PI / 2.21f);
            Vector2 offset = new Vector2(-5, 3);

            var transform = Matrix3Helpers.CreateTransform(offset, angle, scale);
            var expectedInv = Matrix3Helpers.CreateInverseTransform(offset, angle, scale);

            Matrix3x2.Invert(transform, out var invTransform);
            Assert.That(invTransform.EqualsApprox(expectedInv));
        }

        [Test]
        public void TranslateMultiplyTest()
        {
            // Arrange
            var mat1 = Matrix3Helpers.CreateTranslation(new Vector2(1, 1));
            var mat2 = Matrix3Helpers.CreateTranslation(new Vector2(-2, -2));
            var mat3 = Matrix3Helpers.CreateTranslation(new Vector2(3, 3));

            var res2 = Matrix3x2.Multiply(mat1, mat2);
            var res3 = Matrix3x2.Multiply(res2, mat3);

            // Act
            Vector2 test = new Vector2(0, 0);
            var result = Vector2.Transform(test, res3);

            // Assert
            Assert.That(MathHelper.CloseToPercent(result.X, 2), result.ToString);
            Assert.That(MathHelper.CloseToPercent(result.Y, 2), result.ToString);
        }

        [Test]
        public void SpaceSwitchTest()
        {
            // Arrange
            var startPoint = new Vector2(2, 0);
            var rotateMatrix = Matrix3Helpers.CreateRotation((float)(System.Math.PI / 6.3967));
            var translateMatrix = Matrix3Helpers.CreateTranslation(new Vector2(5.357f, -37.53854f));

            // NOTE: Matrix Product is NOT commutative. OpenTK (and this) uses pre-multiplication, OpenGL and all the tutorials
            // you will read about it use post-multiplication. So in OpenTK MVP = M*V*P; in OpenGL it is MVP = P*V*M.
            var transformMatrix = Matrix3x2.Multiply(rotateMatrix, translateMatrix);

            // Act
            var localPoint = Vector2.Transform(startPoint, transformMatrix);

            Matrix3x2.Invert(transformMatrix, out var invMatrix);
            var result = Vector2.Transform(localPoint, invMatrix);

            // Assert
            Assert.That(MathHelper.CloseToPercent(startPoint.X, result.X), Is.True, result.ToString);
            Assert.That(MathHelper.CloseToPercent(startPoint.Y, result.Y), Is.True, result.ToString);
        }

        private static readonly (Box2, Box2)[] TestTransformBoxData =
        {
            (new Box2(-1, -1, 1, 1), new Box2(8.718287f, 8.718287f, 11.281713f, 11.281713f)),
            (new Box2(0, 0, 1, 1), new Box2(10, 9.65798f, 11.281713f, 10.9396925f)),
        };

        [Test]
        public void TestTransformBox([ValueSource(nameof(TestTransformBoxData))] (Box2 box, Box2 expected) set)
        {
            var (box, expected) = set;

            var matrix = Matrix3Helpers.CreateRotation(Angle.FromDegrees(-20));
            matrix.M31 += 10;
            matrix.M32 += 10;

            var transformed = matrix.TransformBox(box);

            Assert.That(transformed, Is.Approximately(expected));
        }
    }
}
