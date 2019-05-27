using System;
using JetBrains.Annotations;

namespace Robust.Shared.IoC
{
    /// <summary>
    ///     The sole purpose of this factory is to create arbitrary objects that have their
    ///     dependencies resolved. If you think you need Activator.CreateInstance(), use this
    ///     factory instead.
    /// </summary>
    [PublicAPI]
    public interface IDynamicTypeFactory
    {
        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        ///     The type MUST have a parameterless constructor.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstance(Type type);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <typeparam name="T">Type of object to instantiate.</typeparam>
        /// <returns>Newly created object.</returns>
        T CreateInstance<T>()
            where T : new();
    }

    /// <inheritdoc />
    internal class DynamicTypeFactory : IDynamicTypeFactory
    {
        // https://blog.ploeh.dk/2012/03/15/ImplementinganAbstractFactory/

        #pragma warning disable 649
        [Dependency]
        private readonly IDependencyCollection _dependencies;
        #pragma warning restore 649

        /// <inheritdoc />
        public object CreateInstance(Type type)
        {
            if(type == null)
                throw new ArgumentNullException(nameof(type));

            var instance = Activator.CreateInstance(type);
            _dependencies.InjectDependencies(instance);
            return instance;
        }

        /// <inheritdoc />
        public T CreateInstance<T>()
            where T : new()
        {
            var instance = new T();
            _dependencies.InjectDependencies(instance);
            return instance;
        }
    }
}
