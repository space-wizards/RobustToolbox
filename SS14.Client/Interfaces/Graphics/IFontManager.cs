using System;
using SS14.Client.Graphics;

namespace SS14.Client.Interfaces.Graphics
{
    public interface IFontManager
    {

    }

    internal interface IFontManagerInternal : IFontManager
    {
        IFontFaceHandle Load(ReadOnlySpan<byte> data);
        IFontInstanceHandle MakeInstance(IFontFaceHandle handle, int size);
        void Initialize();
    }

    internal interface IFontFaceHandle
    {

    }

    internal interface IFontInstanceHandle
    {
        Texture GetCharTexture(char chr);
        CharMetrics? GetCharMetrics(char chr);
        int Ascent { get; }
        int Descent { get; }
        int Height { get; }
    }

    public readonly struct CharMetrics
    {
        public readonly int BearingX;
        public readonly int BearingY;
        public readonly int Advance;

        public CharMetrics(int bearingX, int bearingY, int advance)
        {
            BearingX = bearingX;
            BearingY = bearingY;
            Advance = advance;
        }
    }
}
