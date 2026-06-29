using System.Collections.Generic;

namespace Robust.Client.Graphics.FontManagement;

/// <summary>
/// A fallback implementation of <see cref="ISystemFontManager"/> that just loads no fonts.
/// </summary>
internal sealed class SystemFontManagerFallback : ISystemFontManagerInternal
{
    public void Initialize()
    {

    }

    public void Shutdown()
    {

    }

    public bool IsSupported => false;
    public IEnumerable<ISystemFontFace> SystemFontFaces => [];
}
