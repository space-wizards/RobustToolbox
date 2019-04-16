namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Approximate equality checking, to handle floating point errors.
    /// </summary>
    public interface IApproxEquatable<in T>
    {
        bool EqualsApprox(T other);
        bool EqualsApprox(T other, double tolerance);
    }
}
