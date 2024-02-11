using System;
using System.Runtime.Serialization;

namespace Robust.Shared.Prototypes;

[Serializable]
[Virtual]
public class PrototypeLoadException : Exception
{
    public PrototypeLoadException()
    {
    }

    public PrototypeLoadException(string message) : base(message)
    {
    }

    public PrototypeLoadException(string message, Exception inner) : base(message, inner)
    {
    }
}

[Serializable]
[Virtual]
public class UnknownPrototypeException : Exception
{
    public override string Message => $"Unknown {Kind.Name} prototype: {Prototype}" ;
    public readonly string Prototype;
    public readonly Type Kind;

    public UnknownPrototypeException(string prototype, Type kind)
    {
        Prototype = prototype;
        Kind = kind;
    }
}
