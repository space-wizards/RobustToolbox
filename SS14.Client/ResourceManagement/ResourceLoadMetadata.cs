using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Basically contains everything the resource cache is capable of collecting about a resource being loaded,
    ///     to pass off to the resource loader itself.
    /// </summary>
    public class ResourceLoadMetadata
    {
        public readonly string DiskPath;
    }
}
