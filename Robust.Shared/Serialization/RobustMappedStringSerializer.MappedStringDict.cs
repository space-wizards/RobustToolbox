using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using NetSerializer;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    internal partial class RobustMappedStringSerializer
    {
        internal sealed class MappedStringDict
        {
            private readonly ISawmill _sawmill;
            public bool Locked { get; set; }

            // All the mapped strings.
            // The dict is an array of indices into the array.
            private string[]? _mappedStrings;
            private FrozenDictionary<string, int>? _stringMapping;

            // Strings collected while building the mapping. This is released once the mapping is finalized.
            private HashSet<string>? _buildingStrings = new(StringComparer.Ordinal);
            private readonly object _buildingLock = new();

            public int StringCount => _mappedStrings?.Length ?? 0;

            public MappedStringDict(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void FinalizeMapping()
            {
                HashSet<string> buildingStrings;
                lock (_buildingLock)
                {
                    Locked = true;
                    buildingStrings = _buildingStrings ?? throw new InvalidOperationException("Mapped strings have already been finalized.");
                    _buildingStrings = null;
                }

                // Sort to ensure determinism even if addition order is different.
                _mappedStrings = buildingStrings.ToArray();
                Array.Sort(_mappedStrings, StringComparer.Ordinal);

                // Create dictionary.
                _stringMapping = GenMapDict(_mappedStrings);
            }

            private FrozenDictionary<string, int> GenMapDict(string[] strings)
            {
                var dict = new Dictionary<string, int>(strings.Length, StringComparer.Ordinal);
                for (var i = 0; i < strings.Length; i++)
                {
                    dict.Add(strings[i], i);
                }

                var st = RStopwatch.StartNew();
                var frozen = dict.ToFrozenDictionary(StringComparer.Ordinal);
                _sawmill.Verbose($"Freezing mapped strings took {st.Elapsed.TotalMilliseconds:f2}ms");
                return frozen;
            }

            public (byte[] mapHash, byte[] package) GeneratePackage()
            {
                DebugTools.Assert(Locked);
                DebugTools.AssertNotNull(_mappedStrings);

                var memoryStream = new MemoryStream();
                WriteStringPackage(_mappedStrings!, memoryStream, out var hash);
                var package = memoryStream.ToArray();

                return (hash, package);
            }

            public int LoadFromPackage(byte[] package, out byte[] hash)
            {
                var stream = new MemoryStream(package, false);
                return LoadFromPackage(stream, out hash);
            }

            public int LoadFromPackage(Stream stream, out byte[] hash)
            {
                _mappedStrings = ReadStringPackage(stream, out hash);
                _stringMapping = GenMapDict(_mappedStrings);

                return _mappedStrings.Length;
            }

            private static string[] ReadStringPackage(Stream stream, out byte[] hash)
            {
                Span<byte> buf = stackalloc byte[MaxMappedStringSize];
                using var zs = new ZStdDecompressStream(stream, ownStream: false);
                using var hasherStream = Blake2BHasherStream.CreateReader(zs, ReadOnlySpan<byte>.Empty, 32);

                Primitives.ReadPrimitive(hasherStream, out uint count);
                var list = new string[count];

                for (var i = 0; i < count; ++i)
                {
                    Primitives.ReadPrimitive(hasherStream, out uint lu);
                    if (lu > MaxMappedStringSize)
                        throw new InvalidDataException("Mapped string package contains an overly long string.");

                    var l = (int) lu;
                    var span = buf[..l];
                    hasherStream.ReadExact(span);

                    var str = Encoding.UTF8.GetString(span);
                    list[i] = str;
                }

                hash = hasherStream.Finish();
                return list;
            }

            /// <summary>
            /// Writes a strings package to a stream.
            /// </summary>
            /// <param name="stream">A writable stream.</param>
            /// <exception cref="NotImplementedException">Overly long string in strings package.</exception>
            private static void WriteStringPackage(string[] strings, Stream stream, out byte[] hash)
            {
                // ReSharper disable once SuggestVarOrType_Elsewhere
                Span<byte> buf = stackalloc byte[MaxMappedStringSize];

                using var zs = new ZStdCompressStream(stream, ownStream: false);
                using var hasherStream = Blake2BHasherStream.CreateWriter(zs, ReadOnlySpan<byte>.Empty, 32);

                Primitives.WritePrimitive(hasherStream, (uint) strings.Length);

                foreach (var str in strings)
                {
                    int l;
                    try
                    {
                        l = Encoding.UTF8.GetBytes(str, buf);
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidDataException("Attempted to map a string that exceeds the maximum length.", e);
                    }

                    Primitives.WritePrimitive(hasherStream, (uint) l);
                    hasherStream.Write(buf[..l]);
                }

                hash = hasherStream.Finish();
            }


            /// <summary>
            /// Remove all strings from the mapping, completely resetting it.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the mapping is locked.
            /// </exception>
            public void ClearStrings()
            {
                lock (_buildingLock)
                {
                    if (Locked)
                    {
                        throw new InvalidOperationException("Mapped strings are locked, will not clear.");
                    }

                    _buildingStrings = new HashSet<string>(StringComparer.Ordinal);
                    _mappedStrings = null;
                    _stringMapping = null;
                }
            }

            /// <summary>
            /// Add a string to the constant mapping.
            /// </summary>
            /// <remarks>
            /// If the string has multiple detectable subcomponents, such as a
            /// filepath, it may result in more than one string being added to
            /// the mapping. As string parts are commonly sent as subsets or
            /// scoped names, this increases the likelyhood of a successful
            /// string mapping.
            /// </remarks>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the string is not normalized (<see cref="String.IsNormalized()"/>).
            /// </exception>
            public void AddString(string str)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                AddStringCore(str, batch: null);
            }

            private void AddStringCore(string str, HashSet<string>? batch)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return;
                }

                if (!str.IsNormalized())
                {
                    throw new InvalidOperationException("Only normalized strings may be added.");
                }

                if (str.Length >= MaxMappedStringSize) return;

                if (str.Length <= MinMappedStringSize) return;

                str = str.Trim();

                if (str.Length <= MinMappedStringSize) return;

                str = str.Replace(Environment.NewLine, "\n");

                if (str.Length <= MinMappedStringSize) return;

                if (!TryAddString(str, batch))
                {
                    return;
                }

                var symTrimmedStr = str.Trim(TrimmableSymbolChars);
                if (symTrimmedStr != str)
                {
                    AddStringCore(symTrimmedStr, batch);
                }

                if (str.Contains('/'))
                {
                    foreach (var substr in str.Split("/", StringSplitOptions.RemoveEmptyEntries))
                    {
                        AddStringCore(substr, batch);
                    }
                }
                else if (str.Contains("_"))
                {
                    foreach (var substr in str.Split("_", StringSplitOptions.RemoveEmptyEntries))
                    {
                        AddStringCore(substr, batch);
                    }
                }
                else if (str.Contains(" "))
                {
                    foreach (var substr in str.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (substr == str) continue;

                        AddStringCore(substr, batch);
                    }
                }
                else
                {
                    var parts = RxSymbolSplitter.Split(str);
                    foreach (var substr in parts)
                    {
                        if (substr == str) continue;

                        AddStringCore(substr, batch);
                    }

                }
            }

            /// <summary>
            /// Add the constant strings from an <see cref="Assembly"/> to the
            /// mapping.
            /// </summary>
            /// <param name="asm">The assembly from which to collect constant strings.</param>
            public unsafe void AddStrings(Assembly asm)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                if (!asm.TryGetRawMetadata(out var blob, out var len))
                    return;

                var batch = new HashSet<string>(StringComparer.Ordinal);
                var reader = new MetadataReader(blob, len);
                var usrStrHandle = default(UserStringHandle);
                do
                {
                    var userStr = reader.GetUserString(usrStrHandle);
                    if (userStr != "")
                    {
                        // Because these strings are in a loaded assembly they're already interned.
                        // This intern call retrieves the interned instance.
                        AddStringCore(string.Intern(userStr.Normalize()), batch);
                    }

                    usrStrHandle = reader.GetNextHandle(usrStrHandle);
                } while (usrStrHandle != default);

                var strHandle = default(StringHandle);
                do
                {
                    var str = reader.GetString(strHandle);
                    if (str != "")
                    {
                        // Ditto about interning.
                        AddStringCore(string.Intern(str.Normalize()), batch);
                    }

                    strHandle = reader.GetNextHandle(strHandle);
                } while (strHandle != default);
                MergeBatch(batch);
            }

            /// <summary>
            /// Add strings from the given <see cref="YamlStream"/> to the mapping.
            /// </summary>
            /// <remarks>
            /// Strings are taken from YAML anchors, tags, and leaf nodes.
            /// </remarks>
            /// <param name="yaml">The YAML to collect strings from.</param>
            public void AddStrings(YamlStream yaml)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new HashSet<string>(StringComparer.Ordinal);
                foreach (var doc in yaml)
                {
                    foreach (var node in doc.AllNodes)
                    {
                        var a = node.Anchor;
                        if (!a.IsEmpty)
                        {
                            AddStringCore(a.Value, batch);
                        }

                        var t = node.Tag;
                        if (!t.IsEmpty)
                        {
                            AddStringCore(t.Value, batch);
                        }

                        if (node is not YamlScalarNode scalar)
                            continue;

                        var v = scalar.Value;
                        if (string.IsNullOrEmpty(v))
                        {
                            continue;
                        }

                        AddStringCore(v, batch);
                    }
                }
                MergeBatch(batch);
            }

            public void AddStrings(DataNode dataNode)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in DataNodeHelpers.GetAllNodes(dataNode))
                {
                    var t = node.Tag;
                    if (!string.IsNullOrEmpty(t))
                        AddStringCore(t, batch);

                    if (node is not ValueDataNode value)
                        continue;

                    var v = value.Value;
                    if (string.IsNullOrEmpty(v))
                        continue;

                    AddStringCore(v, batch);
                }
                MergeBatch(batch);
            }

            /// <summary>
            /// Add strings from the given enumeration to the mapping.
            /// </summary>
            /// <param name="strings">The strings to add.</param>
            public void AddStrings(IEnumerable<string> strings)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new HashSet<string>(StringComparer.Ordinal);
                foreach (var str in strings)
                {
                    AddStringCore(str, batch);
                }
                MergeBatch(batch);
            }

            private void MergeBatch(HashSet<string> batch)
            {
                lock (_buildingLock)
                {
                    if (Locked)
                        throw new InvalidOperationException("Mapped strings are locked, will not add.");

                    var buildingStrings = _buildingStrings ?? throw new InvalidOperationException("Mapped strings have already been finalized.");
                    buildingStrings.UnionWith(batch);
                }
            }

            private bool TryAddString(string str, HashSet<string>? batch)
            {
                if (str.Length > MaxMappedStringSize || Encoding.UTF8.GetByteCount(str) > MaxMappedStringSize)
                    return false;

                if (batch != null)
                    return batch.Add(str);

                lock (_buildingLock)
                {
                    return _buildingStrings?.Add(str) == true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteMappedString(Stream stream, string? value)
            {
                DebugTools.Assert(Locked);

                if (value == null)
                {
                    Primitives.WritePrimitive(stream, MappedNull);
                    return;
                }

                if (_stringMapping!.TryGetValue(value, out var mapping))
                {
#if DEBUG
                    if (mapping >= _mappedStrings!.Length || mapping < 0)
                    {
                        throw new InvalidOperationException(
                            "A string mapping outside of the mapped string table was encountered.");
                    }
#endif
                    Primitives.WritePrimitive(stream, (uint) mapping + FirstMappedIndexStart);
                    StringsHitMetric.Inc();
                    //Logger.DebugS("szr", $"Encoded mapped string: {value}");
                    return;
                }

                // indicate not mapped
                Primitives.WritePrimitive(stream, UnmappedString);
                Primitives.WritePrimitive(stream, value);
                StringsMissMetric.Inc();
                StringsMissCharsMetric.Inc(value.Length);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReadMappedString(Stream stream, out string? value)
            {
                DebugTools.Assert(Locked);

                Primitives.ReadPrimitive(stream, out uint mapIndex);
                if (mapIndex == MappedNull)
                {
                    value = null;
                    return;
                }

                if (mapIndex == UnmappedString)
                {
                    // not mapped
                    Primitives.ReadPrimitive(stream, out value);
                    return;
                }

                value = _mappedStrings![(int) mapIndex - FirstMappedIndexStart];
                //Logger.DebugS("szr", $"Decoded mapped string: {value}");
            }
        }
    }
}
