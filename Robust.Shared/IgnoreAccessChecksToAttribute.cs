// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class IgnoresAccessChecksToAttribute : Attribute {

        // ReSharper disable once InconsistentNaming
        private readonly string assemblyName;

        // ReSharper disable once ConvertToAutoProperty
        // ReSharper disable once UnusedMember.Global
        public string AssemblyName
            => assemblyName;

        public IgnoresAccessChecksToAttribute(string assemblyName)
            => this.assemblyName = assemblyName;

    }

}
