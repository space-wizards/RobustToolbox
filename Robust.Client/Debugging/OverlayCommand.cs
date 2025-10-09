using System;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Client.Debugging;

// Must be internal, due to CreateInstanceUnchecked
[ToolshedCommand]
internal sealed class OverlayCommand : ToolshedCommand
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IDynamicTypeFactoryInternal _factory = default!;

    [CommandImplementation("toggle")]
    internal void Toggle([CommandArgument(customParser:typeof(ReflectionTypeParser<Overlay>))] Type overlay)
    {
        if (!overlay.IsSubclassOf(typeof(Overlay)))
            throw new ArgumentException("Type must be a subclass of overlay");

        if (_overlay.HasOverlay(overlay))
            Remove(overlay);
        else
            Add(overlay);
    }

    [CommandImplementation("add")]
    internal void Add([CommandArgument(customParser: typeof(ReflectionTypeParser<Overlay>))] Type overlay)
    {
        if (!overlay.IsSubclassOf(typeof(Overlay)))
            throw new ArgumentException("Type must be a subclass of overlay");

        if (!overlay.HasParameterlessConstructor())
            throw new ArgumentException("Type must have parameterless constructor");

        if (_overlay.HasOverlay(overlay))
            return;

        // TODO OVERLAYS Give overlays the ContentAccessAllowedAttribute?
        // Then this doesn't need to be unchecked.
        var instance = (Overlay) _factory.CreateInstanceUnchecked(overlay, oneOff: true);
        if (instance is IPostInjectInit init)
            init.PostInject();

        _overlay.AddOverlay(instance);
    }

    [CommandImplementation("remove")]
    public void Remove([CommandArgument(customParser: typeof(ReflectionTypeParser<Overlay>))] Type overlay)
    {
        _overlay.RemoveOverlay(overlay);
    }
}
