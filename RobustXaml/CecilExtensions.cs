using System;
using Mono.Cecil;

namespace RobustXaml;

/// <summary>
/// Source: https://github.com/jbevain/cecil/blob/master/Test/Mono.Cecil.Tests/Extensions.cs
/// </summary>
public static class CecilExtensions
{
    /// <summary>
    /// Specialize some generic parameters of a reference to a generic method. The return value
    /// is a more monomorphized version.
    /// </summary>
    /// <param name="self">the original MethodReference</param>
    /// <param name="arguments">the specialized arguments</param>
    /// <returns>the monomorphic MethodReference</returns>
    /// <exception cref="ArgumentException">if the number of passed arguments is wrong</exception>
    public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference[] arguments)
    {
        if (self.GenericParameters.Count != arguments.Length)
            throw new ArgumentException();

        var instance = new GenericInstanceMethod(self);
        foreach (var argument in arguments)
        {
            instance.GenericArguments.Add(argument);
        }

        return instance;
    }
}
