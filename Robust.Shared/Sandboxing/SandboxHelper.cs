using System;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.Shared.Sandboxing
{
    public interface ISandboxHelper
    {
        /// <summary>
        ///     Effectively equivalent to <see cref="Activator.CreateInstance(Type)"/> but safe for content use.
        /// </summary>
        /// <exception cref="SandboxArgumentException">
        /// Thrown if <paramref name="type"/> is not defined in content.
        /// </exception>
        /// <seealso cref="IDynamicTypeFactory.CreateInstance(System.Type)"/>
        object CreateInstance(Type type);
    }

    internal sealed class SandboxHelper : ISandboxHelper
    {
        [Dependency] private readonly IModLoader _modLoader = default!;

        public object CreateInstance(Type type)
        {
            if (!_modLoader.IsContentTypeAccessAllowed(type))
            {
                throw new SandboxArgumentException("Creating non-content types is not allowed.");
            }

            return Activator.CreateInstance(type)!;
        }
    }
}
