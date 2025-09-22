﻿using System;
using System.Buffers;
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

            // HashSet<string> of strings that we are currently building.
            // This should be added to in a thread-safe manner with TryAddString during building.
            private readonly HashSet<string> _buildingStrings = new();

            public int StringCount => _mappedStrings?.Length ?? 0;

            public MappedStringDict(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void FinalizeMapping()
            {
                Locked = true;

                // Sort to ensure determinism even if addition order is different.
                _mappedStrings = _buildingStrings.ToArray();
                Array.Sort(_mappedStrings);

                // Create dictionary.
                _stringMapping = GenMapDict(_mappedStrings);
            }

            private FrozenDictionary<string, int> GenMapDict(string[] strings)
            {
                var dict = new Dictionary<string, int>();
                for (var i = 0; i < strings.Length; i++)
                {
                    dict.Add(strings[i], i);
                }

                var st = RStopwatch.StartNew();
                var frozen = dict.ToFrozenDictionary();
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
                var buf = ArrayPool<byte>.Shared.Rent(4096);
                using var zs = new ZStdDecompressStream(stream, ownStream: false);
                using var hasherStream = Blake2BHasherStream.CreateReader(zs, ReadOnlySpan<byte>.Empty, 32);

                Primitives.ReadPrimitive(hasherStream, out uint count);
                var list = new string[count];

                for (var i = 0; i < count; ++i)
                {
                    Primitives.ReadPrimitive(hasherStream, out uint lu);
                    var l = (int) lu;
                    var span = buf.AsSpan(0, l);
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
                    // Ok so the code checks the goddamn string size before encoding to UTF-8 to check length.
                    // Yes, this code sucks, but I don't care to fix it right now.
                    if (str.Length > MaxMappedStringSize || Encoding.UTF8.GetByteCount(str) > MaxMappedStringSize)
                        throw new Exception("Attempted to map a string that exceeds the maximum length.");

                    var l = Encoding.UTF8.GetBytes(str, buf);

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
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not clear.");
                }

                _buildingStrings.Clear();
                _mappedStrings = null;
                _stringMapping = null;
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

                if (!TryAddString(str))
                {
                    return;
                }

                var symTrimmedStr = str.Trim(TrimmableSymbolChars);
                if (symTrimmedStr != str)
                {
                    AddString(symTrimmedStr);
                }

                if (str.Contains('/'))
                {
                    foreach (var substr in str.Split("/", StringSplitOptions.RemoveEmptyEntries))
                    {
                        AddString(substr);
                    }
                }
                else if (str.Contains("_"))
                {
                    foreach (var substr in str.Split("_", StringSplitOptions.RemoveEmptyEntries))
                    {
                        AddString(substr);
                    }
                }
                else if (str.Contains(" "))
                {
                    foreach (var substr in str.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (substr == str) continue;

                        AddString(substr);
                    }
                }
                else
                {
                    var parts = RxSymbolSplitter.Split(str);
                    foreach (var substr in parts)
                    {
                        if (substr == str) continue;

                        AddString(substr);
                    }

                    for (var si = 0; si < parts.Length; ++si)
                    {
                        for (var sl = 1; sl <= parts.Length - si; ++sl)
                        {
                            // Don't ask me what the original was doing; we just skip by si and take sl
                            // Can be reduced even further if you know what you're doin
                            var end = si + sl;
                            var subBetter = string.Concat(parts[si..^(parts.Length - end)]);
                            AddString(subBetter);
                        }
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

                var reader = new MetadataReader(blob, len);
                var usrStrHandle = default(UserStringHandle);
                do
                {
                    var userStr = reader.GetUserString(usrStrHandle);
                    if (userStr != "")
                    {
                        // Because these strings are in a loaded assembly they're already interned.
                        // This intern call retrieves the interned instance.
                        AddString(string.Intern(userStr.Normalize()));
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
                        AddString(string.Intern(str.Normalize()));
                    }

                    strHandle = reader.GetNextHandle(strHandle);
                } while (strHandle != default);
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

                foreach (var doc in yaml)
                {
                    foreach (var node in doc.AllNodes)
                    {
                        var a = node.Anchor;
                        if (!a.IsEmpty)
                        {
                            AddString(a.Value);
                        }

                        var t = node.Tag;
                        if (!t.IsEmpty)
                        {
                            AddString(t.Value);
                        }

                        if (node is not YamlScalarNode scalar)
                            continue;

                        var v = scalar.Value;
                        if (string.IsNullOrEmpty(v))
                        {
                            continue;
                        }

                        AddString(v);
                    }
                }
            }

            public void AddStrings(DataNode dataNode)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                foreach (var node in DataNodeHelpers.GetAllNodes(dataNode))
                {
                    var t = node.Tag;
                    if (!string.IsNullOrEmpty(t))
                        AddString(t);

                    if (node is not ValueDataNode value)
                        continue;

                    var v = value.Value;
                    if (string.IsNullOrEmpty(v))
                        continue;

                    AddString(v);
                }
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

                foreach (var str in strings)
                {
                    AddString(str);
                }
            }

            private bool TryAddString(string str)
            {
                if (str.Length > MaxMappedStringSize || Encoding.UTF8.GetByteCount(str) > MaxMappedStringSize)
                    return false;

                // Yes this spends like half the CPU time of AddString in lock contention.
                // But it's still faster than all my other attempts, so...
                lock (_buildingStrings)
                {
                    return _buildingStrings.Add(str);
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
