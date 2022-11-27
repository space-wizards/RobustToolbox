using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class RequiredFieldNotMappedException : Exception
{
    public RequiredFieldNotMappedException(Type type, string field) : base($"Required field {field} of type {type} wasn't mapped.")
    {
    }
}
