using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Robust.Shared.Serialization;

public sealed class YamlNoDocEndDotsFix : IEmitter
{
    private readonly IEmitter _next;

    public YamlNoDocEndDotsFix(IEmitter next)
    {
        this._next = next;
    }

    public void Emit(ParsingEvent @event)
    {
        _next.Emit(@event is DocumentEnd ? new DocumentEnd(true) : @event);
    }
}
