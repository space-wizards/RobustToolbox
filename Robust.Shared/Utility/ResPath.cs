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
[PublicAPI]
[Serializable]
[NetSerializable]
public readonly struct ResPath : IEquatable<ResPath>
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
    ///     Internal system independent path. It uses `/` internally as
    ///     separator and will translate to it on creation.
    /// </summary>
    internal readonly string CanonicalResource;

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
        if (separator == '.') throw new ArgumentException("Separator may not be .  Prefer \\ or /");

        if (path == "" || path == ".")
        {
            CanonicalResource = ".";
            return;
        }

        var sb = new StringBuilder(path.Length);
        var segments = path.Segments(separator).ToArray();
        if (path[0] == separator) sb.Append('/');

        var needsSeparator = false;
        foreach (var segment in segments)
        {
            if ((segment == "." && segments.Length != 0) || segment == "") continue;

            if (needsSeparator) sb.Append('/');

            sb.Append(segment);
            needsSeparator = true;
        }

        CanonicalResource = sb.Length == 0 ? "." : sb.ToString();
    }

    private ResPath(string canonicalResource)
    {
        CanonicalResource = canonicalResource;
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


    public bool IsDirectory()
    {
        return CanonicalResource[^1] == '/';
    }

    #region Operators & Equality

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
        if (right.IsRooted()) return right;

        if (right.IsSelf) return left;

        return new ResPath(left.CanonicalResource + "/" + right.CanonicalResource);
    }

    public static ResPath operator /(ResPath left, string right)
    {
        return new(left.CanonicalResource + "/" + new ResPath(right));
    }
    #endregion

    #region WithMethods

    public object WithExtension(string newExt)
    {
        throw new NotImplementedException();
    }

    public ResPath WithName(string name)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Roots & Relatives

    public bool IsRooted()
    {
        return CanonicalResource[0] == '/';
    }

    public bool IsRelative()
    {
        return !IsRooted();
    }

    /// <summary>
    ///     Returns the path of how this instance is "relative" to <paramref name="basePath" />,
    ///     such that <c>basePath/result == this</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var path1 = new ResourcePath("/a/b/c");
    ///     var path2 = new ResourcePath("/a");
    ///     Console.WriteLine(path1.RelativeTo(path2)); // prints "b/c".
    ///     </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown if we are not relative to the base path.</exception>
    public ResPath RelativeTo(ResPath basePath)
    {
        if (TryRelativeTo(basePath, out var relative)) return relative.Value;

        throw new ArgumentException($"{CanonicalResource} does not start with {basePath}.");
    }

    /// <summary>
    ///     Try pattern version of <see cref="RelativeTo(ResPath)" />.
    /// </summary>
    /// <param name="basePath">The base path which we can be made relative to.</param>
    /// <param name="relative">The path of how we are relative to <paramref name="basePath" />, if at all.</param>
    /// <returns>True if we are relative to <paramref name="basePath" />, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown if the separators are not the same.</exception>
    public bool TryRelativeTo(ResPath basePath, [NotNullWhen(true)] out ResPath? relative)
    {
        if (this == basePath)
        {
            relative = Self;
            return true;
        }

        if (CanonicalResource.StartsWith(basePath.CanonicalResource))
        {
            var x = CanonicalResource[basePath.CanonicalResource.Length..]
                .TrimStart('/');
            relative = new ResPath(x);
            return true;
        }

        relative = null;
        return false;
    }


    /// <summary>
    ///     Turns the path into a rooted path by prepending it with the separator.
    ///     Does nothing if the path is already rooted.
    /// </summary>
    /// <seealso cref="IsRooted" />
    /// <seealso cref="ToRelativePath" />
    public ResPath ToRootedPath()
    {
        return IsRooted()
            ? this
            : new ResPath("/" + CanonicalResource);
    }

    /// <summary>
    ///     Turns the path into a relative path by removing the root separator, if any.
    ///     Does nothing if the path is already relative.
    /// </summary>
    /// <seealso cref="IsRelative" />
    /// <seealso cref="ToRootedPath" />
    public ResPath ToRelativePath()
    {
        if (IsRelative()) return this;

        return this == Root
            ? Self
            : new ResPath(CanonicalResource[1..]);
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

    #endregion

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
        if (newSeparator is "." or "\0") throw new ArgumentException("New separator can't be `.` or `NULL`");

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
    public IEnumerable<string> EnumerateSegments()
    {
        return CanonicalResource.Segments('/');
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

    /// <inheritdoc />
    public bool MoveNext()
    {
        _pos += _len;
        if (++_pos > _owner.Length) return false;

        var ind = _owner.IndexOf(_separator, _pos);
        _len = ind == -1
            ? _owner.Length - _pos
            : ind - _pos;

        return _pos < _owner.Length && _pos + _len <= _owner.Length;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _pos = _owner.Length > 1 && _owner[0] == _separator ? 0 : -1;
        _len = 0;
    }

    /// <inheritdoc />

    object IEnumerator.Current => Current;

    /// <inheritdoc />

    public string Current => _owner.AsSpan(_pos, _len).ToString();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

public static class ResPathExtension
{
    public static IEnumerable<string> Segments(this string resPath, char separator)
    {
        var iter = new SegmentEnumerator(resPath, separator);
        while (iter.MoveNext()) yield return iter.Current;
    }
}