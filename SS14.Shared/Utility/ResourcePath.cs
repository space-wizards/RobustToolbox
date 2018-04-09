using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Because System.IO.Path sucks.
namespace SS14.Shared.Utility
{
    /// <summary>
    ///     Provides object-oriented path manipulation for resource paths.
    ///     ResourcePaths are immutable.
    /// </summary>
    public class ResourcePath
    {
#if WINDOWS
        public const string SYSTEM_SEPARATOR = "\\";
#else
        public const string SYSTEM_SEPARATOR = "/";
#endif

        /// <summary>
        ///     "." as a static. Separator used is <c>/</c>.
        /// </summary>
        public static readonly ResourcePath Self = new ResourcePath(".");

        /// <summary>
        ///     "/" (root) as a static. Separator used is <c>/</c>.
        /// </summary>
        public static readonly ResourcePath Root = new ResourcePath("/");

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
                Segments = new string[] { "." };
                return;
            }

            var segments = new List<string>();
            var splitsegments = path.Split(new string[] { separator }, StringSplitOptions.None);
            var i = 0;
            if (splitsegments[0] == "")
            {
                i = 1;
                segments.Add(separator);
            }
            for (; i < splitsegments.Length; i++)
            {
                var segment = splitsegments[i];
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
            Segments = segments.ToArray();
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
        ///     Returns a new instance with a different separator set.
        /// </summary>
        /// <param name="newSeparator">The new separator to use.</param>
        public ResourcePath ChangeSeparator(string newSeparator)
        {
            // Convert the segments into a string path, then re-parse it.
            // Solves the edge case of the segments containing the new separator.
            var path = new ResourcePath(Segments, newSeparator).ToString();
            return new ResourcePath(path, newSeparator);
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
        ///     Adds a new segments to the path as string.
        /// </summary>
        public static ResourcePath operator /(ResourcePath path, string b)
        {
            return path / new ResourcePath(b, path.Separator);
        }

        /// <summary>
        ///     "Cleans" the resource path, removing any empty segments and resolving . and ..
        /// </summary>
        /// <remarks>
        ///     If .. appears at the base of a path, it is left alone. If it appears at roo level (like /..) it is removed entirely.
        /// </remarks>
        public ResourcePath Clean()
        {
            var segments = new List<string>();

            foreach (var segment in Segments)
            {
                // If you have ".." cleaning that up doesn't remove that.
                if (segment == ".." && segment.Length != 0)
                {
                    // Trying to do /.. results in /
                    if (segments.Count == 1 && segments[0] == Separator)
                    {
                        continue;
                    }

                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(segment);
                }
            }

            return new ResourcePath(segments.ToArray(), Separator);
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
        /// <exception cref="ArgumentException">Thrown if we are not relative to the base path.</exception>
        public ResourcePath RelativeTo(ResourcePath basePath)
        {
            if (basePath.Separator != Separator)
            {
                throw new ArgumentException("Separators must be the same.", nameof(basePath));
            }
            if (Segments.Length < basePath.Segments.Length)
            {
                throw new ArgumentException($"{this} does not start with {basePath}.");
            }
            if (Segments.Length == basePath.Segments.Length)
            {
                if (this == basePath)
                {
                    return new ResourcePath(".", Separator);
                }
                else
                {
                    throw new ArgumentException($"{this} does not start with {basePath}.");
                }
            }
            var i = 0;
            for (; i < basePath.Segments.Length; i++)
            {
                if (Segments[i] != basePath.Segments[i])
                {
                    throw new ArgumentException($"{this} does not start with {basePath}.");
                }
            }

            var segments = new string[Segments.Length - basePath.Segments.Length];
            Array.Copy(Segments, basePath.Segments.Length, segments, 0, segments.Length);
            return new ResourcePath(segments, Separator);
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

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Segments.GetHashCode() | Separator.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ResourcePath path && Equals(path);
        }

        /// <summary>
        ///     Checks that we are equal with <paramref name="path"/>.
        ///     This method does NOT clean the paths beforehand, so paths that point to the same location may fail if they are not cleaned beforehand.
        ///     Paths are never equal if they do not have the same separator.
        /// </summary>
        /// <param name="path">The path to check equality with.</param>
        /// <returns>True if the paths are equal, false otherwise.</returns>
        public bool Equals(ResourcePath path)
        {
            return path.Separator == Separator && Segments.SequenceEqual(path.Segments);
        }

        public static bool operator ==(ResourcePath a, ResourcePath b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ResourcePath a, ResourcePath b)
        {
            return !a.Equals(b);
        }
    }
}
