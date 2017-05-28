using System.Reflection;
using System.IO;
using System;

namespace SS14.Shared.Utility
{
    public static class AssemblyHelpers
    {
        public static Assembly RelativeLoadFrom(string path)
        {
            string assemblyDir = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
            return Assembly.LoadFrom(Path.Combine(assemblyDir, path));
        }
    }
}
