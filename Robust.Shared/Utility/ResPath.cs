using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility;

[PublicAPI, Serializable, NetSerializable]
public struct ResPath : IEquatable<ResPath>
{
    /// <summary>
    ///     The separator for the file system of the system we are compiling to.
    ///     Backslash on Windows, forward slash on sane systems.
    /// </summary>
#if WINDOWS
    public const string SystemSeparator = "\\";
#else
        public const string SystemSeparator = "/";
#endif

    /// <summary>
    ///     "." as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly string Self = ".";

    /// <summary>
    ///     "/" (root) as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly string Root = "/";

    internal string CanonicalResource;


    /// <summary>
    ///     Create a new path from a string, splitting it by the separator provided.
    /// </summary>
    /// <param name="path">The string path to turn into a resource path.</param>
    /// <param name="separator">The separator for the resource path.</param>
    /// <exception cref="ArgumentException">Thrown if you try to use "." as separator.</exception>
    /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
    public ResPath(string path = ".", char separator = '/')
    {
        if (separator == '.' )
        {
            throw new ArgumentException("Separator may not be .  Prefer \\ or /");
        }

        if (path == "" || path == Self)
        {
            CanonicalResource = Self;
            return;
        }

        var sb = new StringBuilder(path.Length);
        var segments = path.Segments(separator).ToArray();
        if (path[0] == separator)
        {
            sb.Append('/');
        }

        var needsSeparator = false;
        foreach (var segment in segments)
        {
            if ((segment == "." && segments.Length != 0 ) || segment == "")
            {
                continue;
            }

            if (needsSeparator)
            {
                sb.Append('/');
            }

            sb.Append(segment);
            needsSeparator = true;
        }
        
        CanonicalResource = sb.Length == 0 ? Self : sb.ToString();

    }

    public string Directory(string separator = SystemSeparator)
    {
        if (IsSelf) return Self;

        int ind = CanonicalResource.LastIndexOf('/');
        return ind != -1
            ? CanonicalResource[..ind]
            : ".";
    }

    public bool IsSelf => CanonicalResource == Self;

    public string Extension
    {
        get
        {
            var filename = Filename;
            if (filename == "") return "";

            var ind = filename.LastIndexOf('.') + 1;
            return ind <= 1
                ? ""
                : filename[ind..];
        }
    }

    public string FilenameWithoutExtension()
    {
        var filename = Filename;

        if (filename == "") return "";
        var ind = filename.LastIndexOf('.');
        return ind <= 0
            ? filename
            : filename[..ind];
    }

    public string Filename
    {
        get
        {
            if (CanonicalResource == Self || CanonicalResource == "")
                return Self;

            // CanonicalResource[..^1] avoids last char if its a folder, it won't matter if 
            // it's a filename
            // Uses +1 to skip `/` found in or starts from beginning of string
            // if we found nothing (ind == -1)
            var ind = CanonicalResource[..^1].LastIndexOf('/') + 1;
            return IsDirectory()
                ? CanonicalResource[ind .. ^1] // Omit last `/`  
                : CanonicalResource[ind..];
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDirectory()
    {
        return CanonicalResource[^1] == '/';
    }

    public bool Equals(ResPath other)
    {
        return CanonicalResource == other.CanonicalResource;
    }

    public override bool Equals(object? obj)
    {
        return obj is ResPath other && Equals(other);
    }

    public override int GetHashCode()
    {
        return CanonicalResource.GetHashCode();
    }

    public static bool operator ==(ResPath left, ResPath right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResPath left, ResPath right)
    {
        return !left.Equals(right);
    }

    public static ResPath operator /(ResPath left, ResPath right)
    {
        return new ResPath(left.CanonicalResource + "/" + right.CanonicalResource);
    }

    public static ResPath operator /(ResPath left, string right)
    {
        return new ResPath(left.CanonicalResource + "/" + right);
    }

    public ResPath ToRelativePath()
    {
        throw new NotImplementedException();
    }

    public object WithExtension(string newExt)
    {
        throw new NotImplementedException();
    }

    public ResPath WithName(string name)
    {
        throw new NotImplementedException();
    }

    public ResPath RelativeTo(ResPath basePath)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsRooted()
    {
        return CanonicalResource[0] == '/';
    }

    public ResPath ToRootedPath()
    {
        throw new NotImplementedException();
    }

    public bool IsRelative()
    {
        throw new NotImplementedException();
    }

    public ResPath Clean()
    {
        throw new NotImplementedException();
    }

    public bool IsClean()
    {
        throw new NotImplementedException();
    }

    public string ChangeSeparator(string newSeparator)
    {
        if (newSeparator is "." or "\0")
        {
            throw new ArgumentException("New separator can't be `.` or `NULL`");
        }

        return newSeparator == "/"
            ? CanonicalResource
            : CanonicalResource.Replace("/", newSeparator);
    }


    public ResPath CommonBase(ResPath basePath)
    {
        throw new NotImplementedException();
    }
}

public struct SegmentEnumerator : IEnumerator<string>
{
    private readonly string _owner;
    private int _pos;
    private int _len;
    private readonly char _separator;

    public SegmentEnumerator(string resPath, char separator = '/')
    {
        _owner = resPath;
        _separator = separator;
        _pos = _owner.Length > 1 && _owner[0] == _separator ? 0 : -1;
        _len = 0;
    }

    public bool MoveNext()
    {
        _pos += _len;
        if (++_pos > _owner.Length)
            return false;

        var ind = _owner.IndexOf(_separator, _pos);
        _len = ind == -1
            ? _owner.Length - _pos
            : ind - _pos;

        return _pos < _owner.Length && _pos + _len <= _owner.Length;
    }

    public void Reset()
    {
        _pos = _owner.Length > 1 && _owner[0] == _separator ? 0 : -1;
        _len = 0;
    }

    object IEnumerator.Current => Current;

    public string Current => _owner.AsSpan(_pos, _len).ToString();


    public void Dispose()
    {
    }
}

public struct ReverseSegmentEnumerator : IEnumerator<string>
{
    private readonly ResPath _owner;
    private int _positionEnd;
    private int _len;

    public ReverseSegmentEnumerator(ResPath resPath)
    {
        _owner = resPath;
        _positionEnd = resPath.CanonicalResource.Length - 1;
        _len = 0;
    }

    public bool MoveNext()
    {
        if (_positionEnd - _len <= 0)
        {
            return false;
        }

        _positionEnd -= _len;
        if (_owner.CanonicalResource[_positionEnd] == '/')
        {
            _positionEnd--;
        }

        var ind = _owner.CanonicalResource[.._positionEnd].LastIndexOf('/');
        if (ind == -1)
        {
            _len = _positionEnd++;
            _positionEnd = 0;
        }
        else
        {
            _len = _positionEnd - ind;
            _positionEnd = ind + 1;
        }


        // skip last `/`
        return _positionEnd >= 0 || _len > 0;
    }

    public void Reset()
    {
        _positionEnd = 0;
        _len = 0;
    }

    public string Current => _owner.CanonicalResource.AsSpan(_positionEnd, _len).ToString();

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}

public static class ResPathExtension
{
    public static IEnumerable<string> Segments(this string resPath, char separator)
    {
        var iter = new SegmentEnumerator(resPath, separator);
        while (iter.MoveNext())
        {
            yield return iter.Current;
        }
    }
}