using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    // Big fucking TODO: Wrap all of this into the existing TransformComponent
    // This was only kept around to make porting easier; the main reason for that is some stuff like sweeps manipulates
    // The transform repeatedly so this was just easier so we can get something up and running first before optimising
    // \ cleaning up.
    public struct PhysicsTransform
    {
        // TODO: Should we use this orrr just use the radians? idk...
        public Complex Quarternion;
        public Vector2 Position;

        public static PhysicsTransform Identity { get; } = new PhysicsTransform(Vector2.Zero, Complex.One);

        /// <summary>
        /// Initialize using a position vector and a Complex rotation.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="rotation">The rotation</param>
        public PhysicsTransform(Vector2 position, Complex rotation)
        {
            Quarternion = rotation;
            Position = position;
        }

        /// <summary>
        /// Initialize using a position vector and a rotation.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="angle">The rotation angle</param>
        public PhysicsTransform(Vector2 position, float angle)
            : this(position, Complex.FromAngle(angle))
        {
        }

        public static Vector2 Multiply(Vector2 left, ref PhysicsTransform right)
        {
            return Multiply(ref left, ref right);
        }

        public static Vector2 Multiply(ref Vector2 left, ref PhysicsTransform right)
        {
            // Opt: var result = Complex.Multiply(left, right.q) + right.p;
            return new Vector2(
                (left.X * right.Quarternion.Real - left.Y * right.Quarternion.Imaginary) + right.Position.X,
                (left.Y * right.Quarternion.Real + left.X * right.Quarternion.Imaginary) + right.Position.Y);
        }

        public static Vector2 Divide(Vector2 left, ref PhysicsTransform right)
        {
            return Divide(ref left, ref right);
        }

        public static Vector2 Divide(ref Vector2 left, ref PhysicsTransform right)
        {
            // Opt: var result = Complex.Divide(left - right.p, right);
            float px = left.X - right.Position.X;
            float py = left.Y - right.Position.Y;
            return new Vector2(
                (px * right.Quarternion.Real + py * right.Quarternion.Imaginary),
                (py * right.Quarternion.Real - px * right.Quarternion.Imaginary));
        }

        public static void Divide(Vector2 left, ref PhysicsTransform right, out Vector2 result)
        {
            // Opt: var result = Complex.Divide(left - right.p, right);
            float px = left.X - right.Position.X;
            float py = left.Y - right.Position.Y;
            result.X = (px * right.Quarternion.Real + py * right.Quarternion.Imaginary);
            result.Y = (py * right.Quarternion.Real - px * right.Quarternion.Imaginary);
        }

        public static PhysicsTransform Multiply(ref PhysicsTransform left, ref PhysicsTransform right)
        {
            return new PhysicsTransform(
                    Complex.Multiply(left.Position, ref right.Quarternion) + right.Position,
                    Complex.Multiply(left.Quarternion, right.Quarternion));
        }

        public static PhysicsTransform Divide(ref PhysicsTransform left, ref PhysicsTransform right)
        {
            return new PhysicsTransform(
                Complex.Divide(left.Position - right.Position, ref right.Quarternion),
                Complex.Divide(left.Quarternion, right.Quarternion));
        }

        public static void Divide(ref PhysicsTransform left, ref PhysicsTransform right, out PhysicsTransform result)
        {
            Complex.Divide(left.Position - right.Position, ref right.Quarternion, out result.Position);
            Complex.Divide(left.Quarternion, right.Quarternion, out result.Quarternion);
        }

        public static void Multiply(ref PhysicsTransform left, Complex right, out PhysicsTransform result)
        {
            result.Position = Complex.Multiply(left.Position, ref right);
            result.Quarternion = Complex.Multiply(left.Quarternion, right);
        }

        public static void Divide(ref PhysicsTransform left, Complex right, out PhysicsTransform result)
        {
            result.Position = Complex.Divide(left.Position, ref right);
            result.Quarternion = Complex.Divide(left.Quarternion, right);
        }
    }
}
