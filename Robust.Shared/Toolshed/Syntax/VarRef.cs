using System;
using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed class VarRef<T>
{
    private Block<T>? _innerBlock;
    private string? _varName;
    private string? _expression;
    private Vector2i? _refSpan;

    public VarRef(string varName)
    {
        _varName = varName;
    }

    public VarRef(Block<T> innerBlock)
    {
        _innerBlock = innerBlock;
    }

    public T? Evaluate(IInvocationContext ctx)
    {
        if (_varName is not null)
        {
            return (T?)ctx.ReadVar(_varName);
        }
        else if (_innerBlock is not null)
        {
            return _innerBlock.Invoke(null, ctx);
        }
        else
        {
            throw new UnreachableException();
        }
    }

    public void Set(IInvocationContext ctx, T? value)
    {
        if (_innerBlock is not null)
            throw new NotImplementedException();

        ctx.WriteVar(_varName!, value);
    }
}

public record struct BadVarTypeError(Type Got, Type Expected, string VarName) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"Got unexpected type {Got.PrettyName()} in {VarName}, expected {Expected.PrettyName()}");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
