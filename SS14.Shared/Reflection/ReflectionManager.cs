using SS14.Shared.Interfaces.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public IReadOnlyList<Assembly> Assemblies => assemblies;

        public IEnumerable<Type> GetAllChildren<T>(bool inclusive=false)
        {
            return Assemblies.SelectMany(t => t.GetTypes())
                             .Where(t => typeof(T).IsAssignableFrom(t)
                                      && !t.IsAbstract
                                      && ((Attribute.GetCustomAttribute(t, typeof(ReflectAttribute)) as ReflectAttribute)
                                          ?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE)
                                      && (inclusive || typeof(T) != t));
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());
        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            this.assemblies.AddRange(assemblies);
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
    }
}
