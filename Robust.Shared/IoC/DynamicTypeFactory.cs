using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Shared.IoC
{
    /// <summary>
    ///     The sole purpose of this factory is to create arbitrary objects that have their
    ///     dependencies resolved. If you think you need Activator.CreateInstance(), use this
    ///     factory instead.
    /// </summary>
    /// <seealso cref="DynamicTypeFactoryExt"/>
    [PublicAPI]
    public interface IDynamicTypeFactory
    {
        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="args">The arguments to be passed to the constructor, or null for no arguments.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstance(Type type, object?[]? args = null, bool oneOff = false);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <typeparam name="T">Type of object to instantiate.</typeparam>
        /// <returns>Newly created object.</returns>
        T CreateInstance<T>(bool oneOff = false) where T : new();
    }

    internal interface IDynamicTypeFactoryInternal : IDynamicTypeFactory
    {
        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="args">The arguments to be passed to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstanceUnchecked(Type type, object?[]? args = null, bool oneOff = false);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <typeparam name="T">Type of object to instantiate.</typeparam>
        /// <returns>Newly created object.</returns>
        T CreateInstanceUnchecked<T>(bool oneOff = false) where T : new();
    }

    /// <summary>
    ///     Extension methods for <see cref="IDynamicTypeFactory"/>.
    /// </summary>
    public static class DynamicTypeFactoryExt
    {
        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        public static T CreateInstance<T>(
            this IDynamicTypeFactory dynamicTypeFactory,
            Type type,
            object?[]? args = null,
            bool oneOff = false)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstance(type, args, oneOff);
        }

        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        internal static T CreateInstanceUnchecked<T>(
            this IDynamicTypeFactoryInternal dynamicTypeFactory,
            Type type,
            object?[]? args = null,
            bool oneOff = false)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstanceUnchecked(type, args, oneOff);
        }
    }

    /// <inheritdoc />
    internal sealed class DynamicTypeFactory : IDynamicTypeFactoryInternal
    {
        // https://blog.ploeh.dk/2012/03/15/ImplementinganAbstractFactory/

        [Dependency] private readonly IDependencyCollection _dependencies = default!;
        [Dependency] private readonly IModLoader _modLoader = default!;

        public object CreateInstance(Type type, object?[]? args = null, bool oneOff = false)
        {
            ThrowIfDisallowed(type);
            return CreateInstanceUnchecked(type, args, oneOff);
        }

        public T CreateInstance<T>(bool oneOff = false)
            where T : new()
        {
            ThrowIfDisallowed(typeof(T));
            return CreateInstanceUnchecked<T>(oneOff);
        }

        public object CreateInstanceUnchecked(Type type, object?[]? args = null, bool oneOff = false)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var instance = Activator.CreateInstance(type, args)!;
            _dependencies.InjectDependencies(instance, oneOff);
            return instance;
        }

        public T CreateInstanceUnchecked<T>(bool oneOff = false) where T : new()
        {
            var instance = new T();
            _dependencies.InjectDependencies(instance, oneOff);
            return instance;
        }

        [DebuggerHidden]
        private void ThrowIfDisallowed(Type type)
        {
            if (!_modLoader.IsContentTypeAccessAllowed(type))
            {
                throw new SandboxArgumentException("Creating non-content types is not allowed.");
            }
        }
    }
}
