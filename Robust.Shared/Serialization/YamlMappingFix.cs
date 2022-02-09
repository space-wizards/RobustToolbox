using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// TODO: This fix exists because of https://github.com/aaubry/YamlDotNet/issues/409.
    /// Credit: https://stackoverflow.com/a/56452440
    /// </summary>
    public sealed class YamlMappingFix : IEmitter
    {
        private readonly IEmitter _next;

        public YamlMappingFix(IEmitter next)
        {
            _next = next;
        }

        public void Emit(ParsingEvent @event)
        {
            if (@event is MappingStart mapping)
            {
                @event = new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style, mapping.Start, mapping.End);
            }
            _next.Emit(@event);
        }
    }
}
