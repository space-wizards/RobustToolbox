using SS14.Shared.Interfaces.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Log;

namespace SS14.Shared.Reflection
{
    public abstract class ReflectionManager : IReflectionManager
    {
        /// <summary>
        /// Enumerable over prefixes that are added to the type provided to <see cref="GetType(string)"/>
        /// if the type can't be found in any assemblies.
        /// </summary>
        /// <remarks>
        /// First prefix should probably be <code>""</code>.
        /// </remarks>
        protected abstract IEnumerable<string> TypePrefixes { get; }
        private readonly List<Assembly> assemblies = new List<Assembly>();

        public event EventHandler<ReflectionUpdateEventArgs> OnAssemblyAdded;

        public IReadOnlyList<Assembly> Assemblies => assemblies;

        public IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
        {
            try
            {
                // There's very little assemblies, so storing these temporarily is cheap.
                // We need to do it ahead of time so that we can catch ReflectionTypeLoadException HERE,
                // so whoever is using us doesn't have to handle them.
                var TypeLists = new List<Type[]>(Assemblies.Count);
                TypeLists.AddRange(Assemblies.Select(t => t.GetTypes()));

                return TypeLists.SelectMany(t => t)
                                .Where(t => typeof(T).IsAssignableFrom(t)
                                    && !t.IsAbstract
                                    && ((Attribute.GetCustomAttribute(t, typeof(ReflectAttribute)) as ReflectAttribute)
                                        ?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE)
                                    && (inclusive || typeof(T) != t));
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.Error("Caught ReflectionTypeLoadException! Dumping child exceptions:");
                foreach (var inner in e.LoaderExceptions)
                {
                    Logger.Error(inner.ToString());
                }
                throw;
            }
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());
        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            this.assemblies.AddRange(assemblies);
            OnAssemblyAdded?.Invoke(this, new ReflectionUpdateEventArgs(this));
        }

        /// <seealso cref="TypePrefixes"/>
        public Type GetType(string name)
        {
            // The priority in which types are retrieved is based on the TypePrefixes list.
            // This is an implementation detail. If you need it: make a better API.
            foreach (string prefix in TypePrefixes)
            {
                string appendedName = prefix + name;
                foreach (var assembly in Assemblies)
                {
                    var theType = assembly.GetType(appendedName);
                    if (theType != null)
                    {
                        return theType;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Type> FindTypesWithAttribute<T>()
        {
            var types = new List<Type>();

            foreach (var assembly in Assemblies)
            {
                types.AddRange(assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(T))));
            }

            return types;
        }
    }
}
