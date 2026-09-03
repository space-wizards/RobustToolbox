using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Robust.Shared.ColorNaming;

/// <summary>
/// A manager for palettes. Allows mapping named colors to known, fixed values.
/// </summary>
public sealed partial class PaletteManager : IPaletteManagerInternal
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;

    // This should be a FrozenDictionary, but you'd need to freeze the dictionary after reading (only) the PalettePrototypes.
    private Dictionary<string, Color> _colorsByQualifiedName;
    private Dictionary<ProtoId<PalettePrototype>, List<Color>> _colorsByPalette;

    // If true, the _colors* dictionaries shouldn't be changed - they'll be changed on prototype reload.
    // FIXME: references to new color names added/changed values in prototype changes will fail.
    private bool _dictsFrozen;

    /// <inheritdoc cref="PaletteManager"/>
    public PaletteManager()
    {
        _colorsByPalette = new();
        _colorsByQualifiedName = new();
    }

    [PublicAPI]
    public void Initialize()
    {
        _dictsFrozen = true;

        _protoMan.PrototypesReloaded += OnPrototypesReloaded;
    }

    [PublicAPI]
    public Color GetQualifiedColor(string name)
    {
        return _colorsByQualifiedName[name];
    }

    [PublicAPI]
    public bool TryGetQualifiedColor(string name, [NotNullWhen(true)] out Color? color)
    {
        if (!_colorsByQualifiedName.TryGetValue(name, out var namedColor))
        {
            color = null;
            return false;
        }

        color = namedColor;
        return true;
    }

    [PublicAPI]
    public void GetPaletteColors(ProtoId<PalettePrototype> palette, List<Color> colors)
    {
        colors.Clear();

        foreach (var color in _colorsByPalette[palette])
        {
            colors.Add(color);
        }
    }

    [PublicAPI]
    public bool TryGetPaletteColors(ProtoId<PalettePrototype> palette, List<Color> colors)
    {
        colors.Clear();

        if (!_colorsByPalette.TryGetValue(palette, out var paletteColors))
            return false;

        foreach (var color in paletteColors)
        {
            colors.Add(color);
        }
        return true;
    }

    [PublicAPI]
    public Color PickRandomColor(ProtoId<PalettePrototype> palette)
    {
        return _random.Pick(_colorsByPalette[palette]);
    }

    [PublicAPI]
    public bool TryPickRandomColor(ProtoId<PalettePrototype> palette, [NotNullWhen(true)] out Color? color)
    {
        if (!_colorsByPalette.TryGetValue(palette, out var paletteColors))
        {
            color = null;
            return false;
        }

        if (paletteColors.Count < 0)
        {
            color = null;
            return false;
        }

        color = _random.Pick(paletteColors);
        return true;
    }

    [PublicAPI]
    public void Add(PalettePrototype proto)
    {
        // Already initialized, we'll catch this on the prototype reload
        if (_dictsFrozen)
            return;

        _colorsByPalette.Add(proto.ID, proto.Colors.Values.ToList());
        foreach (var (name, color) in proto.Colors)
        {
            _colorsByQualifiedName.Add(proto.ID + "." + name, color);
        }
    }

    /// <summary>
    /// Prototype reload handler, refreshes all references to palette colors.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<PalettePrototype>())
            return;

        var count = _protoMan.Count<PalettePrototype>();

        _colorsByQualifiedName.Clear();
        _colorsByPalette.Clear();
        _colorsByQualifiedName.EnsureCapacity(count);
        _colorsByPalette.EnsureCapacity(count);

        foreach (var palette in _protoMan.EnumeratePrototypes<PalettePrototype>())
        {
            List<Color> paletteColors = new(palette.Colors.Count);
            foreach (var color in palette.Colors)
            {
                paletteColors.Add(color.Value);
                _colorsByQualifiedName.Add(palette.ID + "." + color.Key, color.Value);
            }
            _colorsByPalette[palette.ID] = paletteColors;
        }
    }
}
