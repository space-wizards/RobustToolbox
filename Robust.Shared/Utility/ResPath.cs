using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Serialization;
using ArgumentException = System.ArgumentException;

namespace Robust.Shared.Utility;

/// <summary>
///     Provides object-oriented path manipulation for resource paths.
///     ResPath are immutable, and separator is always `/`
/// </summary>
[PublicAPI, Serializable, NetSerializable]
public readonly struct ResPath : IEquatable<ResPath>
{
    /// <summary>
    ///     The separator for the file system of the system we are compiling to.
    ///     Backslash on Windows, forward slash on sane systems.
    /// </summary>
#if WINDOWS
    public const char SystemSeparator = '\\';

    public const string SystemSeparatorStr = @"\";
#else
    public const char SystemSeparator = '/';
    public const string SystemSeparatorStr = "/";
#endif

    /// <summary>
    /// Normalized separator character. Chosen because <c>/</c> is illegal path
    /// character on Linux and Windows.
    /// </summary>
    public const char Separator = '/';

    /// <summary>
    /// Normalized separator string. Chosen because <c>/</c> is illegal path
    /// character on Linux and Windows.
    /// </summary>
    public const string SeparatorStr = "/";

    /// <summary>
    ///     "." as a static.
    /// </summary>
    public static readonly ResPath Self = new(".");

    /// <summary>
    ///     "/" (root) as a static.
    /// </summary>
    public static readonly ResPath Root = new("/");

    /// <summary>
    ///     "" (empty) as a static.
    /// </summary>
    public static readonly ResPath Empty = new("");

    /// <summary>
    ///     Internal system independent path. It uses `/` internally as
    ///     separator and will translate to it on creation.
    /// </summary>
    public readonly string CanonPath;

    public ResPath(string canonPath)
    {
        // Paths should never have non-standardised directory separators passed in, the caller should have already sanitised it.
        DebugTools.Assert(!canonPath.Contains('\\'));
        CanonPath = canonPath;
    }

    /// <summary>
    /// Needed for serv3
    /// </summary>
    public ResPath() : this("")
    {
    }

    /// <summary>
    ///     Returns true if the path is equal to "."
    /// </summary>
    public bool IsSelf => CanonPath == Self.CanonPath;

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
    [JsonIgnore]
    public ResPath Directory
    {
        get
        {
            if (IsSelf)
            {
                return Self;
            }

            var ind = CanonPath.Length > 1 && CanonPath[^1] == '/'
                ? CanonPath[..^1].LastIndexOf('/')
                : CanonPath.LastIndexOf('/');
            return ind switch
            {
                -1 => Self,
                0 => new ResPath(CanonPath[..1]),
                _ => new ResPath(CanonPath[..ind])
            };
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

            var ind = filename.LastIndexOf('.') + 1;
            return ind <= 1
                ? string.Empty
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
    public string FilenameWithoutExtension
    {
        get
        {
            var filename = Filename;

            var ind = filename.LastIndexOf('.');
            return ind <= 0
                ? filename
                : filename[..ind];
        }
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
            if (CanonPath is "." or "")
            {
                return ".";
            }

            // CanonicalResource[..^1] avoids last char if its a folder, it won't matter if
            // it's a filename
            // Uses +1 to skip `/` found in or starts from beginning of string
            // if we found nothing (ind == -1)
            var ind = CanonPath[..^1].LastIndexOf('/') + 1;
            return CanonPath[^1] == '/'
                ? CanonPath[ind .. ^1] // Omit last `/`
                : CanonPath[ind..];
        }
    }

    #region Operators & Equality

    /// <summary>
    ///     Converts this element to String
    /// </summary>
    /// <returns> System independent representation of path</returns>
    public override string ToString()
    {
        return CanonPath;
    }

    /// <inheritdoc/>
    public bool Equals(ResPath other)
    {
        return CanonPath == other.CanonPath;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ResPath other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return CanonPath.GetHashCode();
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
    // * It's clever, although I got the idea from Python's `pathlib`.
    // * It avoids confusing operator precedence causing you to join two strings,
    //   because path + string + string != path + (string + string),
    //   whereas path / (string / string) doesn't compile.
    public static ResPath operator /(ResPath left, ResPath right)
    {
        if (right.IsRooted)
        {
            return right;
        }

        if (right.IsSelf)
        {
            return left;
        }

        if (left == Root)
        {
            return new ResPath("/" + right.CanonPath);
        }

        // Avoid double separators
        if (left.CanonPath.EndsWith("/"))
        {
            return new ResPath(left.CanonPath + right.CanonPath);
        }

        return new ResPath(left.CanonPath + "/" + right.CanonPath);
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

    /// <summary>
    ///     Return a copy of this resource path with the file extension changed.
    /// </summary>
    /// <param name="newExtension">
    ///     The new file extension.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="newExtension"/> is null, empty,
    ///     contains <c>/</c> or is equal to <c>.</c>
    /// </exception>
    public ResPath WithExtension(string newExtension)
    {
        if (string.IsNullOrEmpty(newExtension))
        {
            throw new ArgumentException("New file name cannot be null or empty.");
        }

        if (newExtension.Contains('/'))
        {
            throw new ArgumentException("New file name cannot contain the separator.");
        }

        return WithName($"{FilenameWithoutExtension}.{newExtension}");
    }

    /// <summary>
    ///     Return a copy of this resource path with the file name changed.
    /// </summary>
    /// <param name="name">
    ///     The new file name.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="name"/> is null, empty,
    ///     contains <c>/</c> or is equal to <c>.</c>
    /// </exception>
    public ResPath WithName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("New file name cannot be null or empty.");
        }

        if (name.Contains('/'))
        {
            throw new ArgumentException("New file name cannot contain the separator.");
        }

        if (name == ".")
        {
            throw new ArgumentException("New file name cannot be '.'");
        }

        return new ResPath(Directory + "/" + name);
    }

    #endregion

    #region Roots & Relatives

    /// <summary>
    ///     Returns true if the path is rooted/absolute (starts with the separator).
    /// </summary>
    /// <seealso cref="IsRelative" />
    /// <seealso cref="ToRootedPath"/>
    public bool IsRooted => CanonPath.Length > 0 && CanonPath[0] == '/';

    /// <summary>
    ///     Returns true if the path is not rooted.
    /// </summary>
    /// <seealso cref="IsRooted" />
    /// <seealso cref="ToRelativePath"/>
    public bool IsRelative => !IsRooted;


    /// <summary>
    ///     Returns the path of how this instance is "relative" to <paramref name="basePath" />,
    ///     such that <c>basePath/result == this</c>.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var path1 = new ResPath("/a/b/c");
    ///     var path2 = new ResPath("/a");
    ///     Console.WriteLine(path1.RelativeTo(path2)); // prints "b/c".
    ///     </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown if we are not relative to the base path.</exception>
    public ResPath RelativeTo(ResPath basePath)
    {
        if (TryRelativeTo(basePath, out var relative))
        {
            return relative.Value;
        }

        throw new ArgumentException($"{CanonPath} does not start with '{basePath}'.");
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

        // "foo.txt" is relative to "."
        if (basePath == Self && IsRelative)
        {
            relative = this;
            return true;
        }

        if (CanonPath.StartsWith(basePath.CanonPath))
        {
            var x = CanonPath[basePath.CanonPath.Length..]
                .Trim('/');
            relative = x == "" ? Self : new ResPath(x);
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
        return IsRooted
            ? this
            : new ResPath("/" + CanonPath);
    }

    /// <summary>
    ///     Turns the path into a relative path by removing the root separator, if any.
    ///     Does nothing if the path is already relative.
    /// </summary>
    /// <seealso cref="IsRelative" />
    /// <seealso cref="ToRootedPath" />
    public ResPath ToRelativePath()
    {
        if (IsRelative)
        {
            return this;
        }

        if (this == Root)
            return Self;

        var newPath = new ResPath(CanonPath[1..]);
        return newPath.IsRelative ? newPath : newPath.ToRelativePath();
    }

    /// <summary>
    ///     Turns the path into a relative path with system-specific separator.
    ///     For usage in disk I/O.
    /// </summary>
    public string ToRelativeSystemPath()
    {
        return ToRelativePath().ChangeSeparator(SystemSeparatorStr);
    }

    /// <summary>
    ///     Converts a system path into a resource path.
    /// </summary>
    public static ResPath FromRelativeSystemPath(string path, char newSeparator = SystemSeparator)
    {
        // ReSharper disable once RedundantArgumentDefaultValue
        return new ResPath(path.Replace(newSeparator, '/'));
    }

    #endregion

    /// <summary>
    ///     Turns the path into a relative path with system-specific separator.
    ///     For usage in disk I/O.
    /// </summary>
    public string ChangeSeparator(string newSeparator)
    {
        if (newSeparator is "." or "\0")
        {
            throw new ArgumentException("New separator can't be `.` or `NULL`");
        }

        return newSeparator == "/"
            ? CanonPath
            : CanonPath.Replace("/", newSeparator);
    }
}

public static class ResPathUtil
{
    /// <summary>
    ///     Returns cleaned version of the resource path, removing <c>..</c>.
    /// </summary>
    /// <remarks>
    ///     If <c>..</c> appears at the base of a path, it is left alone. If it appears at root level (like <c>/..</c>) it is removed entirely.
    /// </remarks>
    public static ResPath Clean(this ResPath path)
    {
        if (path.CanonPath == "")
        {
            return ResPath.Empty;
        }

        var segments = new ValueList<string>();
        if (path.IsRooted)
        {
            segments.Add("/");
        }

        foreach (var segment in path.CanonPath.Split(ResPath.Separator))
        {
            // Skip pointless segments
            if (segment == "." || segment == "")
            {
                continue;
            }

            // If you have ".." cleaning that up doesn't remove that.
            if (segment == ".." && segments.Count > 0)
            {
                if (segments is ["/"])
                {
                    continue;
                }

                var pos = segments.Count - 1;
                if (segments[pos] != "..")
                {
                    segments.RemoveAt(pos);
                    continue;
                }
            }

            segments.Add(segment);
        }

        // Build Canon path from segments with StringBuilder
        var sb = new StringBuilder(path.CanonPath.Length);
        var start = path.IsRooted && segments.Count > 1 ? 1 : 0;
        for (var i = 0; i < segments.Count; i++)
        {
            if (i > start)
            {
                sb.Append('/');
            }

            sb.Append(segments[i]);
        }

        return sb.Length == 0
            ? ResPath.Self
            : new ResPath(sb.ToString());
    }

    /// <summary>
    /// Gets the segments in common with 2 paths.
    /// </summary>
    public static ResPath GetCommonSegments(this ResPath path, ResPath other)
    {
        var segmentsA = path.EnumerateSegments();
        var segmentsB = other.EnumerateSegments();

        var count = Math.Min(segmentsA.Length, segmentsB.Length);
        var common = new ValueList<string>();

        for (var i = 0; i < count; i++)
        {
            if (segmentsA[i] == segmentsB[i])
            {
                common.Add(segmentsA[i]);
                continue;
            }

            break;
        }

        return new ResPath(string.Join(ResPath.Separator, common));
    }

    /// <summary>
    /// Gets the next segment after where the common segments end.
    /// </summary>
    public static ResPath GetNextSegment(this ResPath path, ResPath other)
    {
        var segmentsA = path.EnumerateSegments();
        var segmentsB = other.EnumerateSegments();

        var count = Math.Min(segmentsA.Length, segmentsB.Length);
        var matched = 0;
        var nextSegment = string.Empty;

        for (var i = 0; i < count; i++)
        {
            if (segmentsA[i] == segmentsB[i])
            {
                nextSegment = segmentsA[i];
                matched++;
                continue;
            }

            break;
        }

        if (matched < segmentsA.Length)
        {
            // Is this the easiest way to tell it's a file?
            // Essentially once we know how far we matched we want the next segment along if it exists
            // Also add in the trailing separator if it's a directory.
            nextSegment = segmentsA[matched] + (matched != segmentsA.Length - 1 || path.Extension.Length == 0 ? ResPath.SeparatorStr : string.Empty);
        }

        return new ResPath(nextSegment);
    }

    /// <summary>
    ///   Enumerates segments skipping over first element in
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string[] EnumerateSegments(this ResPath path)
    {
        return path.IsRooted
            ? path.CanonPath[1..].Split(ResPath.Separator)
            : path.CanonPath.Split(ResPath.Separator);
    }
}
