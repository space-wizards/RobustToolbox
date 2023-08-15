using System;
using System.Linq;
using System.Reflection;
using Robust.Shared.Collections;

namespace Robust.Shared.ContentPack
{
    public static class AppDomainExt
    {
        /// <summary>
        ///     Gets an assembly by name from the given AppDomain.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Assembly GetAssemblyByName(this AppDomain domain, string name)
        {
            var assemblies = new ValueList<Assembly>(1);

            foreach (var assembly in domain.GetAssemblies())
            {
                if (assembly.GetName().Name != name)
                    continue;

                assemblies.Add(assembly);
            }

            if (assemblies.Count != 1)
            {
                var assemblyDesc = string.Join(" ", assemblies.Select(o => o.GetName().Name));
                throw new InvalidOperationException($"Expected 1 assembly for {name}, found {assemblies.Count}. Found {assemblyDesc}");
            }

            return assemblies[0];
        }
    }
}
