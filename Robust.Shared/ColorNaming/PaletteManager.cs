using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Robust.Shared.ColorNaming;

public sealed partial class PaletteManager : IPaletteManager
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;

    private FrozenDictionary<string, List<Color>> _colorsByPalette;
    private FrozenDictionary<string, Color> _colorsByQualifiedName;

    private bool _initialized;

    public PaletteManager()
    {
        _colorsByPalette = new Dictionary<string, List<Color>>().ToFrozenDictionary();
        _colorsByQualifiedName = new Dictionary<string, Color>().ToFrozenDictionary();
    }

    [PublicAPI]
    public void Initialize()
    {
        if (_initialized)
            return;

        ReloadPalettes();

        _initialized = true;

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

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<PalettePrototype>())
            return;

        ReloadPalettes();
    }

    private void ReloadPalettes()
    {
        var count = _protoMan.Count<PalettePrototype>();
        Dictionary<string, List<Color>> colorsByPalette = new(count);
        Dictionary<string, Color> colorsByQualifiedName = new(count);

        foreach (var palette in _protoMan.EnumeratePrototypes<PalettePrototype>())
        {
            List<Color> paletteColors = new(palette.Colors.Count);
            foreach (var color in palette.Colors)
            {
                paletteColors.Add(color.Value);
                colorsByQualifiedName.Add(palette.ID + "." + color.Key, color.Value);
            }
            colorsByPalette[palette.ID] = paletteColors;
        }

        _colorsByPalette = colorsByPalette.ToFrozenDictionary();
        _colorsByQualifiedName = colorsByQualifiedName.ToFrozenDictionary();
    }
}
