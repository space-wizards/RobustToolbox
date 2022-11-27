using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class ReadCallReturnedNullException : Exception
{
    public override string Message => "Read-call returned null for non-nullable type!";
}
