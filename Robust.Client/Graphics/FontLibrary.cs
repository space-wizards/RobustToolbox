using System;
using System.Threading;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Utility;
using Robust.Client.ResourceManagement;
namespace Robust.Client.Graphics;

/// <summary>
/// Stores a single style (Bold, Italic, Monospace), or any combination thereof.
/// </summary>
public record FontVariant (FontStyle Style, FontResource[] Resource)
{

    private ReaderWriterLockSlim _rwl = new();
    private byte[] _sizes = new byte[0];
    private Font[] _fonts = new Font[0];

    public virtual Font ToFont(byte size)
    {
        Font ret;
        _rwl.EnterUpgradeableReadLock();
        try {
            var idx = Array.BinarySearch(_sizes, size);
            if (idx >= 0)
                return _fonts[idx];

            _rwl.EnterWriteLock();
            try {
                if (Resource.Length == 1)
                {
                    ret = new VectorFont(Resource[0], size);
                }
                else
                {
                    var fs = new Font[Resource.Length];
                    for (var i = 0; i < Resource.Length; i++)
                        fs[i] = new VectorFont(Resource[i], size);

                    ret = new StackedFont(fs);
                }

                var newF = new Font[_fonts.Length + 1];
                var newS = new byte[_sizes.Length + 1];
                newF[_fonts.Length] = ret;
                newS[_sizes.Length] = size;

                _fonts = newF;
                _sizes = newS;

                Array.Sort(_sizes, _fonts);
            }
            finally
            {
                _rwl.ExitWriteLock();
            }

            return ret;
        }
        finally
        {
            _rwl.ExitUpgradeableReadLock();
        }
    }

    ~FontVariant()
    {
        _rwl.Dispose();
    }
};

internal record DummyVariant(FontStyle fs) : FontVariant(fs, new FontResource[0])
{
    public override Font ToFont(byte size) => new DummyFont();
};

public record FontClass
(
    string Id,
    FontStyle Style,
    FontSize Size
);

/// <summary>
/// Manages font-based bookkeeping across a single stylesheet.
/// </summary>
public interface IFontLibrary
{
    FontClass Default { get; }

    /// <summary>Associates a name to a set of font resources.</summary>
    void AddFont(string name, params FontVariant[] variants);

    /// <summary>Sets a standard size which can be reused across the Font Library.</summary>
    void SetStandardSize(ushort number, byte size);

    /// <summary>Sets a standard style which can be reused across the Font Library.</summary>
    void SetStandardStyle(ushort number, string name, FontStyle style);

    /// <summary>
    /// Returns a fancy handle in to the library.
    /// The handle keeps track of relative changes to <paramref name="fst"/> and <paramref name="fsz"/>.
    /// </summary>
    IFontLibrarian StartFont(string id, FontStyle fst, FontSize fsz);

    IFontLibrarian StartFont(FontClass? fclass = default) =>
        StartFont(
                (fclass ?? Default).Id,
                (fclass ?? Default).Style,
                (fclass ?? Default).Size
        );
}

/// <summary>
/// Acts as a handle in to an <seealso cref="IFontLibrary"/>.
/// </summary>
public interface IFontLibrarian
{
    Font Current { get; }
    Font Update(FontStyle fst, FontSize fsz);
}

public class FontLibrary : IFontLibrary
{
    public FontClass Default { get; set; }

    public FontLibrary(FontClass def)
    {
        Default = def;
    }

    private Dictionary<string, FontVariant[]> _styles = new();
    private Dictionary<FontStyle, (string, FontStyle)> _standardSt = new();
    private Dictionary<FontSize, byte> _standardSz = new();
    private Dictionary<FontClass, Font> _cache = new();

    void IFontLibrary.AddFont(string name, params FontVariant[] variants) =>
        _styles[name] = variants;

    IFontLibrarian IFontLibrary.StartFont(string id, FontStyle fst, FontSize fsz) =>
        new FontLibrarian(this, id, fst, fsz);

    void IFontLibrary.SetStandardStyle(ushort number, string name, FontStyle style) =>
        _standardSt[(FontStyle) number | FontStyle.Standard] = (name, style);

    void IFontLibrary.SetStandardSize(ushort number, byte size) =>
        _standardSz[(FontSize) number | FontSize.Standard] = size;

    private FontVariant lookup(string id, FontStyle fst)
    {
        if (fst.HasFlag(FontStyle.Standard))
            (id, fst) =  _standardSt[fst];

        FontVariant? winner = default;
        foreach (var vr in _styles[id])
        {
            var winfst = winner?.Style ?? ((FontStyle) 0);

            // Since the "style" flags are a bitfield, we can just see which one has more bits.
            // More bits == closer to the desired font style. Free fallback!

            // Variant's bit count
            var vc = BitOperations.PopCount((ulong) (vr.Style & fst));
            // Winner's bit count
            var wc = BitOperations.PopCount((ulong) (winfst & fst));

            if (winner is null || vc > wc)
                winner = vr;
        }

        if (winner is null)
            throw new Exception($"no matching font style ({id}, {fst})");

        return winner;
    }

    private byte lookupSz(FontSize sz)
    {
        if (sz.HasFlag(FontSize.RelMinus) || sz.HasFlag(FontSize.RelPlus))
            throw new Exception("can't look up a relative font through a library; get a Librarian first");

        if (sz.HasFlag(FontSize.Standard))
            return _standardSz[sz];

        return (byte) sz;
    }

    class FontLibrarian : IFontLibrarian
    {
        public Font Current => _current;
        private Font _current;

        private FontLibrary _lib;
        private string _id;
        private FontStyle _fst;
        private FontSize _fsz;

        public FontLibrarian(FontLibrary lib, string id, FontStyle fst, FontSize fsz)
        {
            _id = id;
            _fst = fst;
            _fsz = fsz;
            _lib = lib;

            // Actual font entry
            var f = lib.lookup(id, fst);

            // Real size
            var rsz = (byte) lib.lookupSz(fsz);
            _current = f.ToFont(rsz);
        }

        Font IFontLibrarian.Update(FontStyle fst, FontSize fsz)
        {
            // Are we the same style?
            if (fst == _fst
                    && !(fsz.HasFlag(FontSize.Special) && fsz.HasFlag(FontSize.RelPlus | FontSize.RelMinus)) // Are we not changing relative font sizes?
                    && (fsz == _fsz))
            {
                // Nothing's changed.
                return _current;
            }

            var f = _lib.lookup(_id, fst);

            byte rsz = (byte) _fsz;
            var msk = (byte) fsz & 0b0000_1111;
            if (fsz.HasFlag(FontSize.Standard))
                rsz = _lib.lookupSz(fsz);
            else if (fsz.HasFlag(FontSize.RelPlus))
                rsz = (byte) (((byte) _fsz) + msk);
            else if (fsz.HasFlag(FontSize.RelMinus))
                rsz = (byte) (((byte) _fsz) - msk);

            _fsz = (FontSize) rsz;
            _fst = fst;

            return _current = f.ToFont((byte) rsz);
        }
    }
}
