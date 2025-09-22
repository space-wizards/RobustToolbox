using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class RequiredFieldNotMappedException : Exception
{
    public RequiredFieldNotMappedException(Type type, string field, Type dataDef) : base($"Required field {field} of type {type} in {dataDef} wasn't mapped.")
    {
    }
}
