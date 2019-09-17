namespace Robust.Shared.Animations
{
    /// <summary>
    ///     Specifies that this object has special animation properties
    ///     that are not able to be represented with <see cref="AnimatableAttribute"/>.
    /// </summary>
    public interface IAnimationProperties
    {
        void SetAnimatableProperty(string name, object value);
    }
}
