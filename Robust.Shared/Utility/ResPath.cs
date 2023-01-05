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


    /// <summary>
    /// Private constructor used to quickly concatenate Resources
    /// It assumes Canonical Resource has been cleared via other constructor
    /// </summary>
    /// <param name="canonicalResource"></param>
    private ResPath(string canonicalResource)
    {
        CanonicalResource = canonicalResource;
    }

    /// <summary>
    ///     Returns true if the path is equal to "."
    /// </summary>
    public bool IsSelf => CanonicalResource == Self.CanonicalResource;

    /// <summary>
    ///     Returns the parent directory that this file resides in
    ///     as a <see cref="ResPath"/>.
    ///     If path points to folder, it will return parent directory
    /// </summary>
    /// <example>
    /// <code>
    ///     // Directory property of a directory resourcePath.
    ///     Assert.AreEqual("/foo", new ResPath("/foo/bar").Directory.ToString());
    ///     // Directory of a file resourcePath.
    ///     Assert.AreEqual("/foo", new ResPath("/foo/x.txt").Directory.ToString());
    /// </code>
    /// </example>
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

    /// <summary>
    ///     Returns the file extension of <see cref="ResPath"/>, if any as string.
    ///     Returns "" if there is no file extension. (Hidden) Files starting
    ///     with period (".") are counted as files with no extension.
    ///     The extension returned does NOT include a period.
    /// </summary>
    /// <example>
    /// <code>
    ///     // file with normal extension
    ///     var picPath = new ResPath("/a/b/c.png");
    ///     Assert.AreEqual("png", picPath.Extension);
    ///     // hidden file starting with `.`
    ///     var gitignore = new ResPath("/a/b/.gitignore");
    ///     Assert.AreEqual("", gitignore.Extension);
    /// </code>
    /// </example>
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

    /// <summary>
    ///     Returns the file name part of a <see cref="ResPath"/>, as string.
    ///     In essence reverse of <see cref="Extension"/>.
    ///     If last segment divided of a path (e.g. <c>/foo/bar/baz.png</c>) divided by separator (e.g <c>/</c>)
    ///     is considered a filename (e.g. <c>baz.png</c>). In that segment part before period is
    ///     considered filename (e.g. <c>baz</c>, unless file start with period, then whole segment
    ///     is filename without extension.
    /// </summary>
    /// <example>
    /// <code>
    ///     // file with normal extension
    ///     var picPath = new ResPath("/a/b/foo.png");
    ///     Assert.AreEqual("foo", picPath.FilenameWithoutExtension());
    ///     // hidden file starting with `.`
    ///     var gitignore = new ResPath("/a/b/.gitignore");
    ///     Assert.AreEqual(".gitignore", gitignore.FilenameWithoutExtension());
    /// </code>
    /// </example>
    public string FilenameWithoutExtension()
    {
        var filename = Filename;

        if (filename == "") return "";
        var ind = filename.LastIndexOf('.');
        return ind <= 0
            ? filename
            : filename[..ind];
    }

    /// <summary>
    ///     Returns the file name (folders are files) for given path,
    ///     or "." if path is empty.
    ///     If last segment divided of a path (e.g. <c>/foo/bar/baz.png</c>) divided by separator (e.g <c>/</c>)
    ///     is considered a filename (e.g. <c>baz.png</c>).
    /// </summary>
    /// <example>
    /// <code>
    ///     // file
    ///     Assert.AreEqual("c.png", new ResPath("/a/b/c.png").Filename);
    ///     // folder
    ///     Assert.AreEqual("foo", new ResPath("/foo").Filename);
    ///     // empty
    ///     Assert.AreEqual(".", new ResPath("").Filename);
    /// </code>
    /// </example>
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
            return CanonicalResource[^1] == '/'
                ? CanonicalResource[ind .. ^1] // Omit last `/`  
                : CanonicalResource[ind..];
        }
    }

    #region Operators & Equality

    /// <inheritdoc/>
    public bool Equals(ResPath other)
    {
        return CanonicalResource == other.CanonicalResource;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ResPath other && Equals(other);
    }

    /// <inheritdoc/>
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

    /// <summary>
    ///     Joins two resource paths together, with separator in between.
    ///     If the second path is absolute (i.e. rooted), the first path is completely ignored.
    ///     <seealso cref="IsRooted"/>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the separators of the two paths do not match.</exception>
    // Copied comment
    // "Why use / instead of +" you may think:
    // * It's clever, although I got the idea from Python's pathlib.
    // * It avoids confusing operator precedence causing you to join two strings,
    //   because path + string + string != path + (string + string),
    //   whereas path / (string / string) doesn't compile.
    public static ResPath operator /(ResPath left, ResPath right)
    {
        if (right.IsRooted()) return right;

        if (right.IsSelf) return left;

        return new ResPath(left.CanonicalResource + "/" + right.CanonicalResource);
    }

    /// <summary>
    ///     Joins resource and string path together, by converting string to <see cref="ResPath"/>
    ///     If the second path is absolute (i.e. rooted), the first path is completely ignored.
    ///     <seealso cref="IsRooted"/>
    /// </summary>
    public static ResPath operator /(ResPath left, string right)
    {
        return left / new ResPath(right);
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

    /// <summary>
    ///     Returns true if the path is rooted/absolute (starts with the separator).
    /// </summary>
    /// <seealso cref="IsRelative" />
    /// <seealso cref="ToRootedPath"/>
    public bool IsRooted()
    {
        return CanonicalResource[0] == '/';
    }

    /// <summary>
    ///     Returns true if the path is not rooted.
    /// </summary>
    /// <seealso cref="IsRooted" />
    /// <seealso cref="ToRelativePath"/>
    public bool IsRelative()
    {
        return !IsRooted();
    }

    public ResPath CommonBase(ResPath basePath)
    {
        throw new NotImplementedException();
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

    /// <summary>
    ///     "Cleans" the resource path, removing <c>..</c>.
    /// </summary>
    /// <remarks>
    ///     If <c>..</c> appears at the base of a path, it is left alone. If it appears at root level (like <c>/..</c>) it is removed entirely.
    /// </remarks>
    public ResPath Clean()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Check whether a path is clean, i.e. <see cref="Clean"/> would not modify it.
    /// </summary>
    /// <returns></returns>
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

/// <summary>
/// Iterator over segments in <see cref="ResPath"/> from the root directory to children.
/// For example give path <c>/foo/bar/baz</c> will yield <c>foo</c> then <c>bar</c>, then
/// <c>baz</c>.
/// </summary>
public struct SegmentEnumerator : IEnumerator<string>
{
    /// <summary>
    /// Original path with custom separator
    /// </summary>
    private readonly string _path;

    /// <summary>
    /// Position of head used to extract segment via <see cref="ReadOnlySpan{T}"/>
    /// </summary>
    private int _pos;

    /// <summary>
    /// Length of segment <see cref="_pos"/> is pointing
    /// </summary>
    private int _len;

    /// <summary>
    /// Separator used. Defaults to <c>/</c>.
    /// </summary>
    private readonly char _separator;

    /// <summary>
    /// Construct <see cref="SegmentEnumerator"/>
    /// </summary>
    /// <param name="path">string input representing path</param>
    /// <param name="separator">character used to separate paths. Defaults to <c>/</c></param>
    public SegmentEnumerator(string path, char separator = '/')
    {
        _path = path;
        _separator = separator;
        // Small trick because _pos needs to always point at '/' so on first iteration
        // if path is relative we treat like the separator is before start of string
        _pos = _path.Length > 1 && _path[0] == _separator ? 0 : -1;
        _len = 0;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        // Move Span start _pos by length of last string read
        _pos += _len;
        // Head points now to '/'.
        // Increment to enable string search to work, and abort if position out of range
        if (++_pos > _path.Length)
        {
            return false;
        }

        // Find next segment
        var ind = _path.IndexOf(_separator, _pos);
        // If segment not found, _len must account for the rest of string
        _len = (ind == -1 ? _path.Length : ind) - _pos;

        return _pos < _path.Length && _pos + _len <= _path.Length;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _pos = _path.Length > 1 && _path[0] == _separator ? 0 : -1;
        _len = 0;
    }

    /// <inheritdoc />

    object IEnumerator.Current => Current;

    /// <inheritdoc />

    public string Current => _path.AsSpan(_pos, _len).ToString();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

public static class ResPathExtension
{
    public static IEnumerable<string> Segments(this string path, char separator)
    {
        var iter = new SegmentEnumerator(path, separator);
        while (iter.MoveNext())
        {
            yield return iter.Current;
        }
    }
}