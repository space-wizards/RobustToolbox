using Robust.Shared.IoC;
using System.Collections.Generic;
using Robust.Shared.Reflection;

namespace Robust.Server.Reflection
{
    /// <summary>
    /// Implementation of <see cref="ReflectionManager"/>
    /// that defines <code>Robust.Server</code> and <code>Robust.Shared</code>
    /// as valid prefixes for <see cref="ReflectionManager.GetType(string)"/>
    /// </summary>
    public sealed class ServerReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new[]
        {
            "",
            "Robust.Server.",
            "Robust.Shared.",
            "Content.Shared.",
            "Content.Server."
        };
    }
}
