using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System.Collections.Generic;

namespace SS14.Server.Reflection
{
    /// <summary>
    /// Implementation of <see cref="ReflectionManager"/>
    /// that defines <code>SS14.Server</code> and <code>SS14.Shared</code>
    /// as valid prefixes for <see cref="ReflectionManager.GetType(string)"/>
    /// </summary>
    [IoCTarget]
    public sealed class ServerReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new[] { "", "SS14.Server.", "SS14.Shared." };
    }
}