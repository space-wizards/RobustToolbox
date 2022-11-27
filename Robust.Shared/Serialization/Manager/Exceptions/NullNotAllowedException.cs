using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class NullNotAllowedException : Exception
{
    public override string Message => "Null value provided for reading but type was not nullable!";
}
