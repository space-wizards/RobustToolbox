using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Shared.Reflection
{
    /// <summary>
    /// Controls additional info about discoverability from the <see cref="IReflectionManager"/>.
    /// Note that this attribute is implicit.
    /// Values are assumed to be their default if this attribute is not specified.
    /// </summary>
    /// <seealso cref="IReflectionManager"/>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ReflectAttribute : Attribute
    {
        /// <summary>
        /// Default value for <see cref="Discoverable"/> if the attribute is not specified.
        /// </summary>
        public const bool DEFAULT_DISCOVERABLE = true;

        /// <summary>
        /// Controls whether this type can be seen by the <see cref="IReflectionManager"/>.
        /// If this is false it cannot be, and will be ignored.
        /// </summary>
        /// <value><see cref="DEFAULT_DISCOVERABLE"/></value>
        /// <seealso cref="ReflectAttribute(bool)"/>
        public bool Discoverable { get; }

        /// <summary>
        /// Controls whether or not the type can be discovered.
        /// </summary>
        /// <param name="discoverable">The value that will be assigned to <see cref="Discoverable"/>. True is yes, false is no.</param>
        /// <seealso cref="Discoverable"/>
        public ReflectAttribute(bool discoverable)
        {
            Discoverable = discoverable;
        }
    }
}
