using System;

namespace Robust.Shared.Prototypes;

public static class PrototypeUtility
{
    public static string CalculatePrototypeName(string type)
    {
        const string prototype = "Prototype";
        if (!type.EndsWith(prototype))
            throw new InvalidPrototypeNameException($"Prototype {type} must end with the word Prototype");

        var name = type.AsSpan();
        return $"{char.ToLowerInvariant(name[0])}{name.Slice(1, name.Length - prototype.Length - 1).ToString()}";
    }
}

public sealed class InvalidPrototypeNameException(string message) : Exception(message);
