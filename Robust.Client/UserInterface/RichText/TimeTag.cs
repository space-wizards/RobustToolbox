using System;
using Robust.Client.GameObjects;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class TimeTag : IMarkupTag
{
    public string Name => "time";

    public string TextBefore(MarkupNode node)
    {
        if (node.Attributes.TryGetValue("time", out var time))
            return time.StringValue!;

        return "00:00:00";
    }
}
