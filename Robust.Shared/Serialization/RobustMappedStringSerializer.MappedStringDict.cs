using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NetSerializer;
using Newtonsoft.Json.Linq;
using Robust.Shared.Interfaces.Log;
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
            private Dictionary<string, int>? _stringMapping;

            // When constructing the mapped strings server side, it can be done in multiple threads at once.
            // To avoid lock contention, each thread will lock to add a single list.
            // This then gets flattened when the final list gets made.
            private readonly List<List<string>> _buildingBatches = new List<List<string>>();

            public int StringCount => _mappedStrings?.Length ?? 0;

            public MappedStringDict(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void FinalizeMapping()
            {
                Locked = true;

                // Flatten string mapping, remove duplicates, and sort.
                _mappedStrings = _buildingBatches
                    .SelectMany(p => p)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToArray();

                // Create dictionary.
                _stringMapping = GenMapDict(_mappedStrings);
            }

            private static Dictionary<string, int> GenMapDict(string[] strings)
            {
                var dict = new Dictionary<string, int>();
                for (var i = 0; i < strings.Length; i++)
                {
                    dict.Add(strings[i], i);
                }

                return dict;
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

            public int LoadFromPackage(Stream stream, out byte[] hash)
            {
                _mappedStrings = ReadStringPackage(stream, out hash).ToArray();
                _stringMapping = GenMapDict(_mappedStrings);

                return _mappedStrings.Length;
            }

            private static List<string> ReadStringPackage(Stream stream, out byte[] hash)
            {
                var list = new List<string>();
                var buf = ArrayPool<byte>.Shared.Rent(4096);
                var hasher = IncrementalHash.CreateHash(PackHashAlgo);
                using var zs = new DeflateStream(stream, CompressionMode.Decompress, true);
                using var hasherStream = new HasherStream(zs, hasher, true);

                var count = ReadCompressedUnsignedInt(hasherStream, out _);
                for (var i = 0; i < count; ++i)
                {
                    var l = (int) ReadCompressedUnsignedInt(hasherStream, out _);
                    var y = hasherStream.Read(buf, 0, l);
                    if (y != l)
                    {
                        throw new InvalidDataException($"Could not read the full length of string #{i}.");
                    }

                    var str = Encoding.UTF8.GetString(buf, 0, l);
                    list.Add(str);
                }

                hash = hasher.GetHashAndReset();
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

                var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);

                using (var zs = new DeflateStream(stream, CompressionLevel.Optimal, true))
                {
                    using var hasherStream = new HasherStream(zs, hasher, true);
                    WriteCompressedUnsignedInt(hasherStream, (uint) strings.Length);

                    foreach (var str in strings)
                    {
                        DebugTools.Assert(str.Length < MaxMappedStringSize);

                        var l = Encoding.UTF8.GetBytes(str, buf);

                        if (l >= MaxMappedStringSize)
                        {
                            throw new NotImplementedException("Overly long string in strings package.");
                        }

                        WriteCompressedUnsignedInt(hasherStream, (uint) l);
                        hasherStream.Write(buf[..l]);
                    }
                }

                hash = hasher.GetHashAndReset();
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

                _buildingBatches.Clear();
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
            /// <returns>
            /// <c>true</c> if the string was added to the mapping for the first
            /// time, <c>false</c> otherwise.
            /// </returns>
            /// <exception cref="InvalidOperationException">
            /// Thrown if the string is not normalized (<see cref="String.IsNormalized()"/>).
            /// </exception>
            public void AddSingleString(string str)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new List<string>();
                AddStringInternal(str, batch);
                CommitBatch(batch);
            }

            /// <summary>
            /// Add the constant strings from an <see cref="Assembly"/> to the
            /// mapping.
            /// </summary>
            /// <param name="asm">The assembly from which to collect constant strings.</param>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public unsafe void AddStrings(Assembly asm)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new List<string>();
                var sw = Stopwatch.StartNew();
                if (asm.TryGetRawMetadata(out var blob, out var len))
                {
                    var reader = new MetadataReader(blob, len);
                    var usrStrHandle = default(UserStringHandle);
                    do
                    {
                        var userStr = reader.GetUserString(usrStrHandle);
                        if (userStr != "")
                        {
                            // Because these strings are in a loaded assembly they're already interned.
                            // This intern call retrieves the interned instance.
                            AddStringInternal(string.Intern(userStr.Normalize()), batch);
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
                            AddStringInternal(string.Intern(str.Normalize()), batch);
                        }

                        strHandle = reader.GetNextHandle(strHandle);
                    } while (strHandle != default);
                }

                CommitBatch(batch);

                var added = batch.Count;
                _sawmill.Debug($"Mapping {added} strings from {asm.GetName().Name} took {sw.ElapsedMilliseconds}ms.");
            }

            /// <summary>
            /// Add strings from the given <see cref="YamlStream"/> to the mapping.
            /// </summary>
            /// <remarks>
            /// Strings are taken from YAML anchors, tags, and leaf nodes.
            /// </remarks>
            /// <param name="yaml">The YAML to collect strings from.</param>
            /// <param name="name">The stream name. Only used for logging.</param>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public void AddStrings(YamlStream yaml, string name)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new List<string>();
                var sw = Stopwatch.StartNew();
                foreach (var doc in yaml)
                {
                    foreach (var node in doc.AllNodes)
                    {
                        var a = node.Anchor;
                        if (!string.IsNullOrEmpty(a))
                        {
                            AddStringInternal(a, batch);
                        }

                        var t = node.Tag;
                        if (!string.IsNullOrEmpty(t))
                        {
                            AddStringInternal(t, batch);
                        }

                        if (!(node is YamlScalarNode scalar))
                            continue;

                        var v = scalar.Value;
                        if (string.IsNullOrEmpty(v))
                        {
                            continue;
                        }

                        AddStringInternal(v, batch);
                    }
                }

                CommitBatch(batch);

                var added = batch.Count;
                _sawmill.Debug($"Mapping {added} string candidates from {name} took {sw.ElapsedMilliseconds}ms.");
            }

            /// <summary>
            /// Add strings from the given <see cref="JObject"/> to the mapping.
            /// </summary>
            /// <remarks>
            /// Strings are taken from JSON property names and string nodes.
            /// </remarks>
            /// <param name="obj">The JSON to collect strings from.</param>
            /// <param name="name">The stream name. Only used for logging.</param>
            public void AddStrings(JObject obj, string name)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new List<string>();
                var sw = Stopwatch.StartNew();
                foreach (var node in obj.DescendantsAndSelf())
                {
                    switch (node)
                    {
                        case JValue value:
                        {
                            if (value.Type != JTokenType.String)
                            {
                                continue;
                            }

                            var v = value.Value?.ToString();
                            if (string.IsNullOrEmpty(v))
                            {
                                continue;
                            }

                            AddStringInternal(v, batch);
                            break;
                        }
                        case JProperty prop:
                        {
                            var propName = prop.Name;
                            if (string.IsNullOrEmpty(propName))
                            {
                                continue;
                            }

                            AddStringInternal(propName, batch);
                            break;
                        }
                    }
                }

                CommitBatch(batch);

                var added = batch.Count;
                _sawmill.Debug($"Mapping {added} strings from {name} took {sw.ElapsedMilliseconds}ms.");
            }

            /// <summary>
            /// Add strings from the given enumeration to the mapping.
            /// </summary>
            /// <param name="strings">The strings to add.</param>
            /// <param name="providerName">The source provider of the strings to be logged.</param>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public void AddStrings(IEnumerable<string> strings, string providerName)
            {
                if (Locked)
                {
                    throw new InvalidOperationException("Mapped strings are locked, will not add.");
                }

                var batch = new List<string>();
                foreach (var str in strings)
                {
                    AddStringInternal(str, batch);
                }

                CommitBatch(batch);
                _sawmill.Debug($"Mapping {batch.Count} strings from {providerName}.");
            }

            private static void AddStringInternal(string str, List<string> batch)
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

                var symTrimmedStr = str.Trim(TrimmableSymbolChars);
                if (symTrimmedStr != str)
                {
                    AddStringInternal(symTrimmedStr, batch);
                }

                if (str.Contains('/'))
                {
                    var parts = str.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < parts.Length; ++i)
                    {
                        for (var l = 1; l <= parts.Length - i; ++l)
                        {
                            var subStr = string.Join('/', parts.Skip(i).Take(l));
                            batch.Add(subStr);

                            if (!subStr.Contains('.'))
                            {
                                continue;
                            }

                            var subParts = subStr.Split('.', StringSplitOptions.RemoveEmptyEntries);
                            for (var si = 0; si < subParts.Length; ++si)
                            {
                                for (var sl = 1; sl <= subParts.Length - si; ++sl)
                                {
                                    var subSubStr = string.Join('.', subParts.Skip(si).Take(sl));
                                    // ReSharper disable once InvertIf
                                    batch.Add(subSubStr);
                                }
                            }
                        }
                    }
                }
                else if (str.Contains("_"))
                {
                    foreach (var substr in str.Split("_"))
                    {
                        AddStringInternal(substr, batch);
                    }
                }
                else if (str.Contains(" "))
                {
                    foreach (var substr in str.Split(" "))
                    {
                        if (substr == str) continue;

                        AddStringInternal(substr, batch);
                    }
                }
                else
                {
                    var parts = RxSymbolSplitter.Split(str);
                    foreach (var substr in parts)
                    {
                        if (substr == str) continue;

                        AddStringInternal(substr, batch);
                    }

                    for (var si = 0; si < parts.Length; ++si)
                    {
                        for (var sl = 1; sl <= parts.Length - si; ++sl)
                        {
                            var subSubStr = String.Concat(parts.Skip(si).Take(sl));
                            batch.Add(subSubStr);
                        }
                    }
                }

                batch.Add(str);
            }

            // See normally I would call this method "AddBatch" but "commit batch" sounds cooler than it is.
            private void CommitBatch(List<string> batch)
            {
                lock (_buildingBatches)
                {
                    _buildingBatches.Add(batch);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteMappedString(Stream stream, string? value)
            {
                DebugTools.Assert(Locked);

                if (value == null)
                {
                    WriteCompressedUnsignedInt(stream, MappedNull);
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
                    WriteCompressedUnsignedInt(stream, (uint) mapping + FirstMappedIndexStart);
                    //Logger.DebugS("szr", $"Encoded mapped string: {value}");
                    return;
                }

                // indicate not mapped
                WriteCompressedUnsignedInt(stream, UnmappedString);

                Primitives.WritePrimitive(stream, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ReadMappedString(Stream stream, out string? value)
            {
                DebugTools.Assert(Locked);

                var mapIndex = ReadCompressedUnsignedInt(stream, out _);
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
