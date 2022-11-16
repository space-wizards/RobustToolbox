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

    public PrototypeLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

[Serializable]
[Virtual]
public class UnknownPrototypeException : Exception
{
    public override string Message => "Unknown prototype: " + Prototype;
    public readonly string? Prototype;

    public UnknownPrototypeException(string prototype)
    {
        Prototype = prototype;
    }

    public UnknownPrototypeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Prototype = (string?) info.GetValue("prototype", typeof(string));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("prototype", Prototype, typeof(string));
    }
}
