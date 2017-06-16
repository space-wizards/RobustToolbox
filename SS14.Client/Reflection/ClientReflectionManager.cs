using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System.Collections.Generic;

namespace SS14.Client.Reflection
{
    /// <summary>
    /// Implementation of <see cref="ReflectionManager"/>
    /// that defines <code>SS14.Client</code> and <code>SS14.Shared</code>
    /// as valid prefixes for <see cref="ReflectionManager.GetType(string)"/>
    /// </summary>
    [IoCTarget]
    public sealed class ClientReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new[] {"", "SS14.Client", "SS14.Shared"};
    }
}
