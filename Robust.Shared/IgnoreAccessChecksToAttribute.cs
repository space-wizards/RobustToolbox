using System.Runtime.CompilerServices;

[assembly: IgnoresAccessChecksTo("SixLabors.ImageSharp")]

namespace System.Runtime.CompilerServices {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class IgnoresAccessChecksToAttribute : Attribute {

        // ReSharper disable once InconsistentNaming
        private readonly string assemblyName;

        // ReSharper disable once ConvertToAutoProperty
        public string AssemblyName
            => assemblyName;

        public IgnoresAccessChecksToAttribute(string assemblyName)
            => this.assemblyName = assemblyName;

    }

}
