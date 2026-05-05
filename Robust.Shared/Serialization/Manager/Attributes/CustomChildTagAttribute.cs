using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
/// Defines a YAML shortform for a derived class.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public sealed class CustomChildTagAttribute<T> : Attribute
{
    public string CustomTag;
    internal string CustomType => typeof(T).Name;

    /// <summary>
    /// Defines a YAML shortform for a derived class.
    /// </summary>
    /// <param name="customTag">The custom tag to be used.</param>
    public CustomChildTagAttribute(string customTag)
    {
        CustomTag = customTag;
    }
}
