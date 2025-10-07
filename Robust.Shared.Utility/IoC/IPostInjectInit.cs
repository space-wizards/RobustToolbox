namespace Robust.Shared.IoC
{
    /// <summary>
    /// If implemented on a type instantiated by IoC,
    /// <see cref="IPostInjectInit.PostInject" /> will be called after all dependencies have been injected.
    /// Do not assume any order in the initialization of other managers,
    /// Or the availability of things through <see cref="IDependencyCollection.Resolve{T}()" />
    /// </summary>
    /// <seealso cref="IDependencyCollection" />
    /// <seealso cref="DependencyAttribute" />
    public interface IPostInjectInit
    {
        /// <summary>
        /// Essentially functions as a constructor after dependencies have been injected.
        /// </summary>
        void PostInject();
    }
}
