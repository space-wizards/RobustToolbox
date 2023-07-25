using System;
using System.Linq.Expressions;

namespace Robust.Shared.RTShell;

public sealed partial class RtShellManager
{
    public bool IsTransformableTo(Type left, Type right)
    {
        throw new NotImplementedException();
    }

    public Expression GetTransformer(Type to, Type from, Expression input)
    {
        throw new NotImplementedException();
    }
}
