using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;


namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// Quick attribute to give the prototype its type string.
    /// To prevent needing to instantiate it because interfaces can't declare statics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(IPrototype))]
    [MeansImplicitUse]
    [MeansDataDefinition]
    public sealed class PrototypeAttribute : Attribute
    {
        private readonly string type;
        public string Type => type;
        public readonly int LoadPriority;
        public readonly string LoadBefore;
        public readonly string LoadAfter;

        public PrototypeAttribute(string type, int loadPriority = 1, string loadBefore="" ,string loadAfter= "")
        {
            this.type = type;
            LoadPriority = loadPriority;
            LoadBefore = loadBefore;
            LoadAfter = loadAfter;
        }
    }
}
