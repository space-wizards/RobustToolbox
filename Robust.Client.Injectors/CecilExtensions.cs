using System;
using Mono.Cecil;

namespace Robust.Build.Tasks
{
    /// <summary>
    /// Source: https://github.com/jbevain/cecil/blob/master/Test/Mono.Cecil.Tests/Extensions.cs
    /// </summary>
    public static class CecilExtensions
    {
        public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference [] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException ();

            var instance = new GenericInstanceMethod (self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add (argument);

            return instance;
        }
    }
}
