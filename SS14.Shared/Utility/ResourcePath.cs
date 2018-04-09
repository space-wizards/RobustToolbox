using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        ///     "." as a static.
        /// </summary>
        public static readonly ResourcePath Self = new ResourcePath(".");

        /// <summary>
        ///     "/" as a static.
        /// </summary>
        public static readonly ResourcePath Root = new ResourcePath("/");

        /// <summary>
        ///     List of the segments of the path.
        ///     This is pretty much a split of the input string path by separator,
        ///     except for the root, which is represented as the separator in position #0.
        /// </summary>
        private readonly string[] Segments;

        public string Separator { get; }

        public ResourcePath(string path, string separator="/")
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
            if (path == "")
            {
                Segments = new string[] { "." };
                return;
            }

            var segments = new List<string>();
            var splitsegments = path.Split(new string[] {separator}, StringSplitOptions.None);
            var i = 0;
            if (splitsegments[0] == "")
            {
                i = 1;
                segments.Add(separator);
            }
            for (; i < splitsegments.Length; i++)
            {
                var segment = splitsegments[i];
                if (segment == "" || (segment == "." && segment.Length != 0))
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
                if (i+1 < Segments.Length)
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
        public bool IsRooted => Segments[0] == Separator;

        /// <summary>
        ///     Returns true if the path is not rooted.
        /// </summary>
        /// <seealso cref="IsRooted" />
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
                if (index == 0 || index == -1 || index == filename.Length-1)
                {
                    // The path is a dotfile (like .bashrc),
                    // or there's no period at all,
                    // or the period is at the very end.
                    // Non of these cases are truly an extension.
                    return "";
                }
                return filename.Substring(index+1);
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

                return Segments[Segments.Length-1];
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
                if (index == 0 || index == -1 || index == filename.Length-1)
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
        /// </summary>
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
            return path/new ResourcePath(b, path.Separator);
        }

        /// <summary>
        ///     "Cleans" the resource path, removing any empty segments and resolving . and ..
        /// </summary>
        /// <remarks>
        ///     If .. appears at root level, it is ignored.
        /// </remarks>
        /// <returns></returns>
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

                    segments.RemoveAt(segments.Count-1);
                }
                else
                {
                    segments.Add(segment);
                }
            }

            if (IsRooted)
            {
                segments.Insert(0, "");
            }

            return new ResourcePath(segments.ToArray(), Separator);
        }

        public ResourcePath ToRootedPath()
        {
            if (IsRooted)
            {
                return this;
            }

            var segments = new string[Segments.Length+1];
            Segments.CopyTo(segments, 1);
            segments[0] = Separator;
            return new ResourcePath(segments, Separator);
        }

        public ResourcePath ToRelativePath()
        {
            if (IsRelative)
            {
                return this;
            }

            var segments = new string[Segments.Length-1];
            Array.Copy(Segments, 1, segments, 0, Segments.Length-1);
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
                    return new ResourcePath(".  ", Separator);
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

            var segments = new string[Segments.Length-basePath.Segments.Length];
            Segments.CopyTo(segments, basePath.Segments.Length);
            return new ResourcePath(segments, Separator);
        }

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

        public override int GetHashCode()
        {
            return Segments.GetHashCode() | Separator.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ResourcePath path && Equals(path);
        }

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
