using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility;

/// <summary>
///     Provides object-oriented path manipulation for resource paths.
///     ResourcePaths are immutable.
/// </summary>
[PublicAPI, Serializable, NetSerializable]
public struct ResPath : IEquatable<ResPath>
{
    /// <summary>
    ///     The separator for the file system of the system we are compiling to.
    ///     Backslash on Windows, forward slash on sane systems.
    /// </summary>
#if WINDOWS
    public const char SystemSeparator = '\\';
#else
        public const char SystemSeparator = '/';
#endif

    /// <summary>
    ///     "." as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly ResPath Self = new(".");

    /// <summary>
    ///     "/" (root) as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly ResPath Root = new("/");

    /// <summary>
    ///     Internal system indepenent path. It uses `/` internally as
    ///     separator and will translate to it on creation.
    /// </summary>
    internal string CanonicalResource;

    /// <summary>
    ///     Converts this element to String
    /// </summary>
    /// <returns> System independent representation of path</returns>
    public override string ToString()
    {
        return CanonicalResource;
    }

    /// <summary>
    ///     Create a new path from a string, splitting it by the separator provided.
    /// </summary>
    /// <param name="path">The string path to turn into a resource path.</param>
    /// <param name="separator">The separator for the resource path.</param>
    /// <exception cref="ArgumentException">Thrown if you try to use "." as separator.</exception>
    public ResPath(string path = ".", char separator = '/')
    {
        if (separator == '.')
        {
            throw new ArgumentException("Separator may not be .  Prefer \\ or /");
        }

        if (path == "" || path == ".")
        {
            CanonicalResource = ".";
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
            if ((segment == "." && segments.Length != 0) || segment == "")
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

        CanonicalResource = sb.Length == 0 ? "." : sb.ToString();
    }

    public bool IsSelf => CanonicalResource == Self.CanonicalResource;

    public ResPath Directory
    {
        get
        {
            if (IsSelf) return Self;

            var ind = CanonicalResource.LastIndexOf('/');
            return ind != -1
                ? new ResPath(CanonicalResource[..ind])
                : Self;
        }
    }

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
            if (CanonicalResource is "." or "")
                return ".";

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


    public bool IsDirectory() => CanonicalResource[^1] == '/';

    public bool Equals(ResPath other) => CanonicalResource == other.CanonicalResource;

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
        if (right.IsRooted())
        {
            return right;
        }

        if (right.IsSelf)
        {
            return left;
        }

        return new ResPath(left.CanonicalResource + "/" + right.CanonicalResource);
    }

    public static ResPath operator /(ResPath left, string right) =>
        new(left.CanonicalResource + "/" + new ResPath(right));


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

    public bool IsRooted() => CanonicalResource[0] == '/';

    public bool IsRelative() => !IsRooted();

    public ResPath ToRootedPath()
    {
        throw new NotImplementedException();
    }

    public ResPath ToRelativePath()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Try pattern version of <see cref="RelativeTo(ResPath)"/>.
    /// </summary>
    /// <param name="basePath">The base path which we can be made relative to.</param>
    /// <param name="relative">The path of how we are relative to <paramref name="basePath"/>, if at all.</param>
    /// <returns>True if we are relative to <paramref name="basePath"/>, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown if the separators are not the same.</exception>
    public bool TryRelativeTo(ResourcePath basePath, [NotNullWhen(true)] out ResourcePath? relative)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Turns the path into a relative path with system-specific separator.
    ///     For usage in disk I/O.
    /// </summary>
    public string ToRelativeSystemPath()
    {
        return ToRelativePath().ChangeSeparator(SystemSeparator.ToString());
    }

    /// <summary>
    ///     Converts a relative disk path back into a resource path.
    /// </summary>
    public static ResPath FromRelativeSystemPath(string path, string newSeparator = "/")
    {
        // ReSharper disable once RedundantArgumentDefaultValue
        return new ResPath(path, SystemSeparator);
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

    /// <summary>
    ///     Enumerates the segments of this path.
    /// </summary>
    /// <remarks>
    ///     Segments are returned from highest to deepest.
    ///     For example <c>/a/b</c> will yield <c>a</c> then <c>b</c>.
    ///     No special indication is given for rooted paths,
    ///     so <c>/a/b</c> yields the same as <c>a/b</c>.
    /// </remarks>
    public IEnumerable<string> EnumerateSegments() => CanonicalResource.Segments('/');
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