using System;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Avalonia.Metadata
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    [PublicAPI]
    public sealed class XmlnsDefinitionAttribute : Attribute
    {
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }

        public string XmlNamespace { get; }
        public string ClrNamespace { get; }
    }
}
