using System;
using System.Collections.Generic;
using System.Reflection;

namespace Robust.Shared.Toolshed.Commands.Types;

#if CLIENT_SCRIPTING
[ToolshedCommand]
internal sealed class MethodsCommand : ToolshedCommand
{
    [CommandImplementation("get")]
    public IEnumerable<MethodInfo> Get([PipedArgument] IEnumerable<Type> types)
    {
        foreach (var ty in types)
        {
            foreach (var method in ty.GetMethods())
            {
                yield return method;
            }
        }
    }

    [CommandImplementation("overrides")]
    public IEnumerable<MethodInfo> Overrides([PipedArgument] IEnumerable<Type> types)
    {
        foreach (var ty in types)
        {
            foreach (var method in ty.GetMethods())
            {
                if (method.DeclaringType != ty)
                    yield return method;
            }
        }
    }

    [CommandImplementation("overridesfrom")]
    public IEnumerable<MethodInfo> OverridesFrom([PipedArgument] IEnumerable<Type> types, Type t)
    {
        foreach (var ty in types)
        {
            foreach (var method in ty.GetMethods())
            {
                if (method.DeclaringType == t)
                    yield return method;
            }
        }
    }
}
#endif
