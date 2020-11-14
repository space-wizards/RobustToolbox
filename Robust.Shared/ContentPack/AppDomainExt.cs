using System;
using System.Linq;
using System.Reflection;

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
            return domain.GetAssemblies().Single(assembly => assembly.GetName().Name == name);
        }
    }
}
