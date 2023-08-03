using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Errors;

public sealed class UnhandledExceptionError : IConError
{
    [PublicAPI]
    public Exception Exception;

    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText(Exception.ToString());
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }

    public UnhandledExceptionError(Exception exception)
    {
        Exception = exception;
    }
}
