using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Utility;

namespace SS14.Shared.IoC
{
    /// <inheritdoc />
    internal class DependencyCollection : IDependencyCollection
    {
        /// <summary>
        /// Dictionary that maps the types passed to <see cref="Resolve{T}"/> to their implementation.
        /// </summary>
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// The types interface types mapping to their registered implementations.
        /// This is pulled from to make a service if it doesn't exist yet.
        /// </summary>
        private readonly Dictionary<Type, Type> _resolveTypes = new Dictionary<Type, Type>();

        /// <inheritdoc />
        public void Register<TInterface, TImplementation>(bool overwrite = false)
            where TImplementation : class, TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            if (_resolveTypes.ContainsKey(interfaceType))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException
                    (
                        string.Format("Attempted to register already registered interface {0}. New implementation: {1}, Old implementation: {2}",
                        interfaceType, typeof(TImplementation), _resolveTypes[interfaceType]
                    ));
                }

                if (_services.ContainsKey(interfaceType))
                {
                    throw new InvalidOperationException($"Attempted to overwrite already instantiated interface {interfaceType}.");
                }
            }

            _resolveTypes[interfaceType] = typeof(TImplementation);
        }

        /// <inheritdoc />
        public void Clear()
        {
            foreach (var service in _services.Values.OfType<IDisposable>().Distinct())
            {
                try
                {
                    service.Dispose();
                }
                catch (Exception e)
                {
                    // DON'T use the logger since it might be dead already.
                    System.Console.WriteLine($"Caught exception inside {service.GetType()} dispose! {e}");
                }
            }
            _services.Clear();
            _resolveTypes.Clear();
        }

        /// <inheritdoc />
        [Pure]
        public T Resolve<T>()
        {
            return (T)ResolveType(typeof(T));
        }

        /// <inheritdoc />
        [Pure]
        public object ResolveType(Type type)
        {
            if (_services.TryGetValue(type, out var value))
            {
                return value;
            }

            if (_resolveTypes.ContainsKey(type))
            {
                // If we have the type registered but not created that means we haven't been told to initialize the graph yet.
                throw new InvalidOperationException($"Attempted to resolve type {type} before the object graph for it has been populated.");
            }

            throw new UnregisteredTypeException(type);

        }

        /// <inheritdoc />
        public void BuildGraph()
        {
            // List of all objects we need to inject dependencies into.
            var injectList = new List<object>();

            // First we build every type we have registered but isn't yet built.
            // This allows us to run this after the content assembly has been loaded.
            foreach (KeyValuePair<Type, Type> currentType in _resolveTypes.Where(p => !_services.ContainsKey(p.Key)))
            {
                // Find a potential dupe by checking other registered types that have already been instantiated that have the same instance type.
                // Can't catch ourselves because we're not instantiated.
                // Ones that aren't yet instantiated are about to be and will find us instead.
                KeyValuePair<Type, Type> dupeType = _resolveTypes.FirstOrDefault(p => _services.ContainsKey(p.Key) && p.Value == currentType.Value);

                // Interface key can't be null so since KeyValuePair<> is a struct,
                // this effectively checks whether we found something.
                if (dupeType.Key != null)
                {
                    // We have something with the same instance type, use that.
                    _services[currentType.Key] = _services[dupeType.Key];
                    continue;
                }

                try
                {
                    var instance = Activator.CreateInstance(currentType.Value);
                    _services[currentType.Key] = instance;
                    injectList.Add(instance);
                }
                catch (TargetInvocationException e)
                {
                    throw new ImplementationConstructorException(currentType.Value, e.InnerException);
                }
            }

            // Graph built, go over ones that need injection.
            foreach (var implementation in injectList)
            {
                InjectDependencies(implementation);
            }

            foreach (var injectedItem in injectList.OfType<IPostInjectInit>())
            {
                injectedItem.PostInject();
            }
        }

        /// <inheritdoc />
        public void InjectDependencies(object obj)
        {
            foreach (var field in obj.GetType().GetAllFields()
                            .Where(p => Attribute.GetCustomAttribute(p, typeof(DependencyAttribute)) != null))
            {
                // Not using Resolve<T>() because we're literally building it right now.
                if (!_services.ContainsKey(field.FieldType))
                {
                    throw new UnregisteredDependencyException(obj.GetType(), field.FieldType, field.Name);
                }

                // Quick note: this DOES work with read only fields, though it may be a CLR implementation detail.
                field.SetValue(obj, _services[field.FieldType]);
            }
        }
    }
}
