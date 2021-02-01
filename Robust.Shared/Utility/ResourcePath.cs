// Because System.IO.Path sucks.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Provides object-oriented path manipulation for resource paths.
    ///     ResourcePaths are immutable.
    /// </summary>
    [PublicAPI, Serializable, NetSerializable]
    public sealed class ResourcePath : IEquatable<ResourcePath>, IDeepClone
    {
        /// <summary>
        ///     The separator for the file system of the system we are compiling to.
        ///     Backslash on Windows, forward slash on sane systems.
        /// </summary>
#if WINDOWS
        public const string SYSTEM_SEPARATOR = "\\";
#else
        public const string SYSTEM_SEPARATOR = "/";
#endif

        /// <summary>
        ///     "." as a static. Separator used is <c>/</c>.
        /// </summary>
        public static readonly ResourcePath Self = new(".");

        /// <summary>
        ///     "/" (root) as a static. Separator used is <c>/</c>.
        /// </summary>
        public static readonly ResourcePath Root = new("/");

        /// <summary>
        ///     List of the segments of the path.
        ///     This is pretty much a split of the input string path by separator,
        ///     except for the root, which is represented as the separator in position #0.
        /// </summary>
        private readonly string[] Segments;

        /// <summary>
        ///     The separator between "segments"/"directories" for this path.
        /// </summary>
        public string Separator { get; }

        /// <summary>
        ///     Create a new path from a string, splitting it by the separator provided.
        /// </summary>
        /// <param name="path">The string path to turn into a resource path.</param>
        /// <param name="separator">The separator for the resource path.</param>
        /// <exception cref="ArgumentException">Thrown if you try to use "." as separator.</exception>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
        public ResourcePath(string path, string separator = "/")
        {
            if (separator == ".")
            {
                throw new ArgumentException("Yeah no.", nameof(separator));
            }

            Separator = separator;

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }

            if (path == "")
            {
                Segments = new string[] {"."};
                return;
            }

            var splitSegments = path.Split(new string[] {separator}, StringSplitOptions.None);
            var segments = new List<string>(splitSegments.Length);
            var i = 0;
            if (splitSegments[0] == "")
            {
                i = 1;
                segments.Add(separator);
            }

            for (; i < splitSegments.Length; i++)
            {
                var segment = splitSegments[i];
                if (segment == "" || (segment == "." && segments.Count != 0))
                {
                    continue;
                }

                if (i == 1 && segments[0] == ".")
                {
                    segments[0] = segment;
                }
                else
                {
                    segments.Add(segment);
                }
            }

            Segments = ListToArray(segments);
        }

        private ResourcePath(string[] segments, string separator)
        {
            Segments = segments;
            Separator = separator;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            var i = 0;
            if (IsRooted)
            {
                i = 1;
                builder.Append(Separator);
            }

            for (; i < Segments.Length; i++)
            {
                builder.Append(Segments[i]);
                if (i + 1 < Segments.Length)
                {
                    builder.Append(Separator);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Returns true if the path is rooted (starts with the separator).
        /// </summary>
        /// <seealso cref="IsRelative" />
        /// <seealso cref="ToRootedPath"/>
        public bool IsRooted => Segments[0] == Separator;

        /// <summary>
        ///     Returns true if the path is not rooted.
        /// </summary>
        /// <seealso cref="IsRooted" />
        /// <seealso cref="ToRelativePath"/>
        public bool IsRelative => !IsRooted;

        /// <summary>
        ///     Returns true if the path is equal to "."
        /// </summary>
        public bool IsSelf => Segments.Length == 1 && Segments[0] == ".";

        /// <summary>
        ///     Returns the file extension of file path, if any.
        ///     Returns "" if there is no file extension.
        ///     The extension returned does NOT include a period.
        /// </summary>
        public string Extension
        {
            get
            {
                var filename = Filename;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return "";
                }

                var index = filename.LastIndexOf('.');
                if (index == 0 || index == -1 || index == filename.Length - 1)
                {
                    // The path is a dotfile (like .bashrc),
                    // or there's no period at all,
                    // or the period is at the very end.
                    // Non of these cases are truly an extension.
                    return "";
                }

                return filename.Substring(index + 1);
            }
        }

        /// <summary>
        ///     Returns the file name.
        /// </summary>
        public string Filename
        {
            get
            {
                if (Segments.Length == 1 && IsRooted)
                {
                    return "";
                }

                return Segments[Segments.Length - 1];
            }
        }

        /// <summary>
        ///     Returns the file name, without extension.
        /// </summary>
        public string FilenameWithoutExtension
        {
            get
            {
                var filename = Filename;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return filename;
                }

                var index = filename.LastIndexOf('.');
                if (index == 0 || index == -1 || index == filename.Length - 1)
                {
                    return filename;
                }

                return filename.Substring(0, index);
            }
        }

        /// <summary>
        ///     Returns the directory that this file resides in.
        /// </summary>
        public ResourcePath Directory
        {
            get
            {
                if (IsSelf) return this;

                var fileName = Filename;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var path = ToString();
                    var dir = path.Remove(path.Length - fileName.Length);
                    return new ResourcePath(dir);
                }

                return this;
            }
        }

        /// <summary>
        ///     Returns a new instance with a different separator set.
        /// </summary>
        /// <param name="newSeparator">The new separator to use.</param>
        /// <exception cref="ArgumentException">Thrown if the new separator is "."</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newSeparator"/> is null.</exception>
        public ResourcePath ChangeSeparator(string newSeparator)
        {
            if (newSeparator == ".")
            {
                throw new ArgumentException("Yeah no.", nameof(newSeparator));
            }

            if (newSeparator == null)
            {
                throw new ArgumentNullException(nameof(newSeparator));
            }

            // Convert the segments into a string path, then re-parse it.
            // Solves the edge case of the segments containing the new separator.
            ResourcePath path;
            if (IsRooted)
            {
                var clone = (string[]) Segments.Clone();
                clone[0] = newSeparator;
                path = new ResourcePath(clone, newSeparator);
            }
            else
            {
                path = new ResourcePath(Segments, newSeparator);
            }
            return new ResourcePath(path.ToString(), newSeparator);
        }

        /// <summary>
        ///     Joins two resource paths together, with separator in between.
        ///     If the second path is absolute, the first path is completely ignored.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the separators of the two paths do not match.</exception>
        // "Why use / instead of +" you may think:
        // * It's clever, although I got the idea from Python's pathlib.
        // * It avoids confusing operator precedence causing you to join two strings,
        //   because path + string + string != path + (string + string),
        //   whereas path / (string / string) doesn't compile.
        public static ResourcePath operator /(ResourcePath a, ResourcePath b)
        {
            if (a.Separator != b.Separator)
            {
                throw new ArgumentException("Both separators must be the same.");
            }

            if (b.IsRooted)
            {
                return b;
            }

            if (b.IsSelf)
            {
                return a;
            }

            string[] segments = new string[a.Segments.Length + b.Segments.Length];
            a.Segments.CopyTo(segments, 0);
            b.Segments.CopyTo(segments, a.Segments.Length);
            return new ResourcePath(segments, a.Separator);
        }

        /// <summary>
        ///     Adds a new segment to the path as string.
        /// </summary>
        public static ResourcePath operator /(ResourcePath path, string b)
        {
            return path / new ResourcePath(b, path.Separator);
        }

        /// <summary>
        ///     "Cleans" the resource path, removing <c>..</c>.
        /// </summary>
        /// <remarks>
        ///     If .. appears at the base of a path, it is left alone. If it appears at root level (like /..) it is removed entirely.
        /// </remarks>
        public ResourcePath Clean()
        {
            var segments = new List<string>();

            foreach (var segment in Segments)
            {
                // If you have ".." cleaning that up doesn't remove that.
                if (segment == ".." && segments.Count != 0)
                {
                    // Trying to do /.. results in /
                    if (segments.Count == 1 && segments[0] == Separator)
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

            if (segments.Count == 0)
            {
                return new ResourcePath(".", Separator);
            }

            return new ResourcePath(ListToArray(segments), Separator);
        }

        /// <summary>
        ///     Check whether a path is clean, i.e. <see cref="Clean"/> would not modify it.
        /// </summary>
        /// <returns></returns>
        public bool IsClean()
        {
            for (var i = 0; i < Segments.Length; i++)
            {
                if (Segments[i] == "..")
                {
                    if (IsRooted)
                    {
                        return false;
                    }

                    if (i > 0 && Segments[i - 1] != "..")
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Turns the path into a rooted path by prepending it with the separator.
        ///     Does nothing if the path is already rooted.
        /// </summary>
        /// <seealso cref="IsRooted" />
        /// <seealso cref="ToRelativePath" />
        public ResourcePath ToRootedPath()
        {
            if (IsRooted)
            {
                return this;
            }

            var segments = new string[Segments.Length + 1];
            Segments.CopyTo(segments, 1);
            segments[0] = Separator;
            return new ResourcePath(segments, Separator);
        }

        /// <summary>
        ///     Turns the path into a relative path by removing the root separator, if any.
        ///     Does nothing if the path is already relative.
        /// </summary>
        /// <seealso cref="IsRelative"/>
        /// <seealso cref="ToRootedPath" />
        public ResourcePath ToRelativePath()
        {
            if (IsRelative)
            {
                return this;
            }

            if (Segments.Length == 1)
            {
                // This path is literally just "/"
                return new ResourcePath(".", Separator);
            }

            var segments = new string[Segments.Length - 1];
            Array.Copy(Segments, 1, segments, 0, Segments.Length - 1);
            return new ResourcePath(segments, Separator);
        }

        /// <summary>
        ///     Turns the path into a relative path with system-specific separator.
        ///     For usage in disk I/O.
        /// </summary>
        public string ToRelativeSystemPath()
        {
            return ChangeSeparator(SYSTEM_SEPARATOR).ToRelativePath().ToString();
        }

        /// <summary>
        ///     Converts a relative disk path back into a resource path.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
        public static ResourcePath FromRelativeSystemPath(string path, string newSeparator = "/")
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            return new ResourcePath(path, SYSTEM_SEPARATOR).ChangeSeparator(newSeparator);
        }

        /// <summary>
        ///     Returns the path of how this instance is "relative" to <paramref name="basePath"/>,
        ///     such that <c>basePath/result == this</c>.
        /// </summary>
        /// <example>
        ///     <code>
        ///     var path1 = new ResourcePath("/a/b/c");
        ///     var path2 = new ResourcePath("/a");
        ///     Console.WriteLine(path1.RelativeTo(path2)); // prints "b/c".
        ///     </code>
        /// </example>
        /// <exception cref="ArgumentException">Thrown if we are not relative to the base path or the separators are not the same.</exception>
        public ResourcePath RelativeTo(ResourcePath basePath)
        {
            if (TryRelativeTo(basePath, out var relative))
            {
                return relative;
            }

            throw new ArgumentException($"{this} does not start with {basePath}.");
        }

        /// <summary>
        ///     Try pattern version of <see cref="RelativeTo(ResourcePath)"/>.
        /// </summary>
        /// <param name="basePath">The base path which we can be made relative to.</param>
        /// <param name="relative">The path of how we are relative to <paramref name="basePath"/>, if at all.</param>
        /// <returns>True if we are relative to <paramref name="basePath"/>, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if the separators are not the same.</exception>
        public bool TryRelativeTo(ResourcePath basePath, [NotNullWhen(true)] out ResourcePath? relative)
        {
            if (basePath.Separator != Separator)
            {
                throw new ArgumentException("Separators must be the same.", nameof(basePath));
            }

            if (Segments.Length < basePath.Segments.Length)
            {
                relative = null;
                return false;
            }

            if (Segments.Length == basePath.Segments.Length)
            {
                if (this == basePath)
                {
                    relative = new ResourcePath(".", Separator);
                    return true;
                }
                else
                {
                    relative = null;
                    return false;
                }
            }

            for (var i = 0; i < basePath.Segments.Length; i++)
            {
                if (Segments[i] != basePath.Segments[i])
                {
                    relative = null;
                    return false;
                }
            }

            var segments = new string[Segments.Length - basePath.Segments.Length];
            Array.Copy(Segments, basePath.Segments.Length, segments, 0, segments.Length);
            relative = new ResourcePath(segments, Separator);
            return true;
        }

        /// <summary>
        ///     Gets the common base of two paths.
        /// </summary>
        /// <example>
        ///     <code>
        ///     var path1 = new ResourcePath("/a/b/c");
        ///     var path2 = new ResourcePath("/a/e/d");
        ///     Console.WriteLine(path1.RelativeTo(path2)); // prints "/a".
        ///     </code>
        /// </example>
        /// <param name="other">The other path.</param>
        /// <exception cref="ArgumentException">Thrown if there is no common base between the two paths.</exception>
        public ResourcePath CommonBase(ResourcePath other)
        {
            if (other.Separator != Separator)
            {
                throw new ArgumentException("Separators must match.");
            }

            var i = 0;
            for (; i < Segments.Length && i < other.Segments.Length; i++)
            {
                if (Segments[i] != other.Segments[i])
                {
                    break;
                }
            }

            if (i == 0)
            {
                throw new ArgumentException($"{this} and {other} have no common base.");
            }

            var segments = new string[i];
            Array.Copy(Segments, segments, i);
            return new ResourcePath(segments, Separator);
        }

        /// <summary>
        ///     Return a copy of this resource path with the file name changed.
        /// </summary>
        /// <param name="name">
        ///     The new file name.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="name"/> is null, empty,
        ///     contains <see cref="Separator"/> or is equal to <c>.</c>
        /// </exception>
        public ResourcePath WithName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("New file name cannot be null or empty.");
            }

            if (name.Contains(Separator))
            {
                throw new ArgumentException("New file name cannot contain the separator.");
            }

            if (name == ".")
            {
                throw new ArgumentException("New file name cannot be '.'");
            }

            var newSegments = (string[]) Segments.Clone();
            newSegments[newSegments.Length - 1] = name;

            return new ResourcePath(newSegments, Separator);
        }

        /// <summary>
        ///     Return a copy of this resource path with the file extension changed.
        /// </summary>
        /// <param name="newExtension">
        ///     The new file extension.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="newExtension"/> is null, empty,
        ///     contains <see cref="Separator"/> or is equal to <c>.</c>
        /// </exception>
        public ResourcePath WithExtension(string newExtension)
        {
            if (string.IsNullOrEmpty(newExtension))
            {
                throw new ArgumentException("New file name cannot be null or empty.");
            }

            if (newExtension.Contains(Separator))
            {
                throw new ArgumentException("New file name cannot contain the separator.");
            }

            return WithName($"{FilenameWithoutExtension}.{newExtension}");
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
            if (IsRooted)
            {
                // Skip '/' root.
                return Segments.Skip(1);
            }

            return Segments;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var code = Separator.GetHashCode();
            foreach (var segment in Segments)
            {
                unchecked
                {
                    code = code * 31 + segment.GetHashCode();
                }
            }

            return code;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ResourcePath path && Equals(path);
        }

        /// <summary>
        ///     Checks that we are equal with <paramref name="path"/>.
        ///     This method does NOT clean the paths beforehand, so paths that point to the same location may fail if they are not cleaned beforehand.
        ///     Paths are never equal if they do not have the same separator.
        /// </summary>
        /// <param name="other">The path to check equality with.</param>
        /// <returns>True if the paths are equal, false otherwise.</returns>
        public bool Equals(ResourcePath? other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.Separator != Separator || Segments.Length != other.Segments.Length)
            {
                return false;
            }

            for (var i = 0; i < Segments.Length; i++)
            {
                if (Segments[i] != other.Segments[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(ResourcePath? a, ResourcePath? b)
        {
            if ((object?) a == null)
            {
                return (object?) b == null;
            }

            return a.Equals(b);
        }

        public static bool operator !=(ResourcePath? a, ResourcePath? b)
        {
            return !(a == b);
        }

        // While profiling I found that List<T>.ToArray() is just incredibly slow. No idea why honestly.
        private static string[] ListToArray(List<string> list)
        {
            var array = new string[list.Count];

            for (var i = 0; i < list.Count; i++)
            {
                array[i] = list[i];
            }

            return array;
        }

        public IDeepClone DeepClone()
        {
            return new ResourcePath(IDeepClone.CloneValue(Segments)!, Separator);
        }
    }
}
