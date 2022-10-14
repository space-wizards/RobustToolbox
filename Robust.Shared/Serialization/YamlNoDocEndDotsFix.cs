using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Robust.Shared.Serialization;

/// <summary>
///     So by default, YamlDotNet appends "..." to the end of a serialized document.
///     In the YAML spec, three dots signify the end of a document.
///     If you're serializing a single document, this is pretty much useless. This emitter removes these dots entirely.
/// </summary>
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
