using System;
using System.Reflection;
using System.Linq;

namespace SS14.Shared.Utility
{
    public static class AppDomainHelpers
    {
        public static Assembly GetAssemblyByName(this AppDomain domain, string name)
        {
            return domain.GetAssemblies().
                SingleOrDefault(assembly => assembly.GetName().Name == name);
        }
    }
}
