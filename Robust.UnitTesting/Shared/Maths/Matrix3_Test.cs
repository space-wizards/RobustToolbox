using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestOf(typeof(Matrix3))]
    public sealed class Matrix3_Test
    {
        [Test]
        public void TranslationTest()
        {
            var control = new Vector2(1, 1);
            var matrix = Matrix3.CreateTranslation(control);

            var origin = new Vector3(0, 0, 1);
            Matrix3.Transform(matrix, ref origin);

            var result = origin.Xy;
            Assert.That(control == result, Is.True, result.ToString);
        }

        private static readonly IEnumerable<(Vector2, double)> _rotationTests = new[]
        {
            (new Vector2( 1, 0).Normalized, 0.0),
            (new Vector2( 1, 1).Normalized, 1 * System.Math.PI / 4.0),
            (new Vector2( 0, 1).Normalized, 1 * System.Math.PI / 2.0),
            (new Vector2(-1, 1).Normalized, 3 * System.Math.PI / 4.0),
            (new Vector2(-1, 0).Normalized, 1 * System.Math.PI / 1.0),
            (new Vector2(-1,-1).Normalized, 5 * System.Math.PI / 4.0),
            (new Vector2( 0,-1).Normalized, 3 * System.Math.PI / 2.0),
            (new Vector2( 1,-1).Normalized, 7 * System.Math.PI / 4.0),
        };

        [Test]
        [Sequential]
        public void RotationTest([ValueSource(nameof(_rotationTests))] (Vector2, double) testCase)
        {
            var angle = testCase.Item2;

            var matrix = Matrix3.CreateRotation((float)angle);

            var test = new Vector3(1, 0, 1);
            Matrix3.Transform(matrix, ref test);

            var control = testCase.Item1;
            var result = test.Xy;

            Assert.That(MathHelper.CloseToPercent(control.X, result.X), Is.True, result.ToString);
            Assert.That(MathHelper.CloseToPercent(control.Y, result.Y), Is.True, result.ToString);
        }

        [Test]
        public void MultiplyTransformOrder()
        {
            var startPoint = new Vector3(1, 0, 1);

            Vector2 scale = new Vector2(2, 2);
            Angle angle = new Angle(System.MathF.PI / 2);
            Vector2 offset = new Vector2(-5, -3);

            var scaleMatrix = Matrix3.CreateScale(scale);
            var rotateMatrix = Matrix3.CreateRotation(angle);
            var translateMatrix = Matrix3.CreateTranslation(offset);

            // 1. Take the start point  -> ( 1, 0)
            // 2. Scale it by 2         -> ( 2, 0)
            // 3. Rotate by +90 degrees -> ( 0, 2)
            // 4. Translate by (-5, -3) -> (-5,-1)
            var result = (scaleMatrix * rotateMatrix * translateMatrix) * startPoint;

            Assert.That(result.X, Is.Approximately(-5f));
            Assert.That(result.Y, Is.Approximately(-1f));

            // repeat but with CreateTransform()
            var transform = Matrix3.CreateTransform(offset, angle, scale);
            result = transform * startPoint;
            Assert.That(result.X, Is.Approximately(-5f));
            Assert.That(result.Y, Is.Approximately(-1f));
        }

        [Test]
        public void InverseTransformTest()
        {
            Vector2 scale = new Vector2(2.32f, 2);
            Angle angle = new Angle(System.MathF.PI / 2.21f);
            Vector2 offset = new Vector2(-5, 3);

            var transform = Matrix3.CreateTransform(offset, angle, scale);
            var invTransform = Matrix3.CreateInverseTransform(offset, angle, scale);

            Assert.That(Matrix3.Invert(transform).EqualsApprox(invTransform));
        }

        [Test]
        public void InvertTest()
        {
            const float epsilon = 1.0E-7f;
            var control = Matrix3.Identity;

            // take our matrix
            var normalMatrix = new Matrix3(
                3, 7, 2,
                1, 8, 4,
                2, 1, 9
                );

            // invert it (1/matrix)
            var invMatrix = Matrix3.Invert(normalMatrix);

            // multiply it back together
            Matrix3.Multiply(ref normalMatrix, ref invMatrix, out var leftVerifyMatrix);
            Matrix3.Multiply(ref invMatrix, ref normalMatrix, out var rightVerifyMatrix);

            // these should be the same (A × A-1 = A-1 × A = I)
            Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(rightVerifyMatrix, epsilon));

            // verify matrix == identity matrix (or very close to because float precision)
            Assert.That(leftVerifyMatrix, new ApproxEqualityConstraint(control, epsilon));
        }

        [Test]
        public void TranslateMultiplyTest()
        {
            // Arrange
            var mat1 = Matrix3.CreateTranslation(new Vector2(1, 1));
            var mat2 = Matrix3.CreateTranslation(new Vector2(-2, -2));
            var mat3 = Matrix3.CreateTranslation(new Vector2(3, 3));

            mat1.Multiply(ref mat2, out var res2);
            res2.Multiply(ref mat3, out var res3);

            // Act
            Vector3 test = new Vector3(0, 0, 1);
            Matrix3.Transform(res3, ref test);
            var result = test.Xy;

            // Assert
            Assert.That(MathHelper.CloseToPercent(result.X, 2), result.ToString);
            Assert.That(MathHelper.CloseToPercent(result.Y, 2), result.ToString);
        }

        [Test]
        public void SpaceSwitchTest()
        {
            // Arrange
            var startPoint = new Vector3(2, 0, 1);
            var rotateMatrix = Matrix3.CreateRotation((float)(System.Math.PI / 6.3967));
            var translateMatrix = Matrix3.CreateTranslation(new Vector2(5.357f, -37.53854f));

            // NOTE: Matrix Product is NOT commutative. OpenTK (and this) uses pre-multiplication, OpenGL and all the tutorials
            // you will read about it use post-multiplication. So in OpenTK MVP = M*V*P; in OpenGL it is MVP = P*V*M.
            Matrix3.Multiply(ref rotateMatrix, ref translateMatrix, out var transformMatrix);

            // Act
            Matrix3.Transform(in transformMatrix, in startPoint, out var localPoint);

            var invMatrix = Matrix3.Invert(transformMatrix);
            Matrix3.Transform(in invMatrix, in localPoint, out var result);

            // Assert
            Assert.That(MathHelper.CloseToPercent(startPoint.X, result.X), Is.True, result.ToString);
            Assert.That(MathHelper.CloseToPercent(startPoint.Y, result.Y), Is.True, result.ToString);
        }
    }
}
