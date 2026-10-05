using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using NetSerializer;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Serialization
{
    // Tests NetSerializer itself because we have specific modifications.
    // e.g. (at the time of writing) list serialization being more compact.
    [Parallelizable(ParallelScope.All)]
    internal sealed class NetSerializer_Test
    {
        public static readonly List<int>?[] ListValues =
        {
            null,
            new List<int>(),
            new List<int> {1, 2, 3},
        };

        [Test]
        public void TestList([ValueSource(nameof(ListValues))] List<int>? list)
        {
            var serializer = new Serializer(new[] {typeof(List<int>)});
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, list);
            stream.Position = 0;

            serializer.DeserializeDirect<List<int>?>(stream, out var deserialized);
            if (list == null)
            {
                Assert.That(deserialized, Is.Null);
            }
            else
            {
                Assert.That(deserialized, Is.EquivalentTo(list));
            }
        }

        public static readonly Dictionary<string, int>?[] DictionaryValues =
        {
            null,
            new Dictionary<string, int>(),
            new Dictionary<string, int> {{"A", 1}},
            new Dictionary<string, int> {{"A", 1}, {"B", 2}, {"C", 3}},
        };

        [Test]
        public void TestDictionary([ValueSource(nameof(DictionaryValues))] Dictionary<string, int>? list)
        {
            var serializer = new Serializer(new[] {typeof(Dictionary<string, int>)});
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, list);
            stream.Position = 0;

            serializer.DeserializeDirect<Dictionary<string, int>?>(stream, out var deserialized);
            if (list == null)
            {
                Assert.That(deserialized, Is.Null);
            }
            else
            {
                Assert.That(deserialized, Is.EquivalentTo(list));
            }
        }

        [Test]
        public void TestGeneratedComponentDeltaStateOnlySerializesChangedFields()
        {
            var serializer = new Serializer(
                new[] {typeof(TestGeneratedComponentDeltaState)},
                new Settings
                {
                    CustomTypeSerializers = [new NetComponentDeltaStateSerializer()],
                });

            var delta = new TestGeneratedComponentDeltaState
            {
                ChangedFields = (1UL << 0) | (1UL << 2),
                Count = 5,
                Name = "should not be serialized",
                Enabled = true,
            };

            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, delta);
            stream.Position = 0;

            serializer.DeserializeDirect<TestGeneratedComponentDeltaState>(stream, out var read);

            Assert.Multiple(() =>
            {
                Assert.That(read.ChangedFields, Is.EqualTo(delta.ChangedFields));
                Assert.That(read.Count, Is.EqualTo(delta.Count));
                Assert.That(read.Enabled, Is.EqualTo(delta.Enabled));
                Assert.That(read.Name, Is.Null);
            });
        }

        public static readonly HashSet<int>?[] HashSetValues =
        {
            null,
            new HashSet<int>(),
            new HashSet<int> {1, 2, 3},
        };

        [Test]
        public void TestHashSet([ValueSource(nameof(HashSetValues))] HashSet<int>? set)
        {
            var serializer = new Serializer(new[] {typeof(HashSet<int>)});
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, set);
            stream.Position = 0;

            serializer.DeserializeDirect<HashSet<int>?>(stream, out var deserialized);
            if (set == null)
            {
                Assert.That(deserialized, Is.Null);
            }
            else
            {
                Assert.That(deserialized, Is.EquivalentTo(set));
            }
        }

        public static readonly Vector2[] Vector2Values =
        {
            Vector2.Zero,
            Vector2.One,
            Vector2Helpers.NaN,
            Vector2Helpers.Infinity,
            new(-1f, -1f),
            new(10f, -10f),
            new(-10f, 10f),
        };

        [Test]
        public void TestVector2([ValueSource(nameof(Vector2Values))] Vector2 vec)
        {
            var serializer = new Serializer(new[] {typeof(Vector2)}, new Settings()
            {
                CustomTypeSerializers = new ITypeSerializer[]
                {
                    new NetMathSerializer(),
                }
            });
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, vec);
            stream.Position = 0;
            serializer.DeserializeDirect(stream, out Vector2 read);

            Assert.That(read, NUnit.Framework.Is.EqualTo(vec));
        }

        [Serializable, NetSerializable]
        private sealed class TestGeneratedComponentDeltaState : IAutoGeneratedComponentDeltaState
        {
            public ulong ChangedFields { get; set; }

            [NetworkedDeltaField(0)]
            public int Count;

            [NetworkedDeltaField(1)]
            public string? Name;

            [NetworkedDeltaField(2)]
            public bool Enabled;

            public void ApplyToFullState(IComponentState fullState)
            {
            }

            public IComponentState CreateNewFullState(IComponentState fullState)
            {
                return fullState;
            }
        }

        [Test]
        [TestCase(0f)]
        [TestCase(-0f)]
        [TestCase(1f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.MaxValue)]
        [TestCase(float.MinValue)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(MathF.PI)]
        public void TestFloats(float f)
        {
            var stream = new MemoryStream();
            Primitives.WritePrimitive(stream, f);
            stream.Position = 0;
            Primitives.ReadPrimitive(stream, out float read);

            Assert.That(read, NUnit.Framework.Is.EqualTo(f));
        }

        [Test]
        [TestCase(0d)]
        [TestCase(-0d)]
        [TestCase(1d)]
        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.MaxValue)]
        [TestCase(double.MinValue)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(Math.PI)]
        public void TestDoubles(double d)
        {
            var stream = new MemoryStream();
            Primitives.WritePrimitive(stream, d);
            stream.Position = 0;
            Primitives.ReadPrimitive(stream, out double read);

            Assert.That(read, NUnit.Framework.Is.EqualTo(d));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("ABC")]
        // ReSharper disable StringLiteralTypo
        [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce arcu mi, vehicula at nunc sit amet, lobortis interdum eros. Ut nec tincidunt odio. Etiam at odio id mauris condimentum elementum eu sit amet sem. Donec libero ex, viverra non metus ut, imperdiet varius lectus. Vivamus ultrices orci sed urna cursus, vel cursus velit lacinia. Mauris pellentesque tristique metus, et iaculis est tincidunt at. Integer maximus elit quis mollis sodales. Sed luctus quam a tempus vulputate.")]
        [TestCase("H̟̟̱̺͉͡o̭̲̱̲͜nk̰̤̙͍͕̘")]
        [TestCase("🤔 U+1F914")]
        [TestCase("壮健")]
        // These emojis are very wide so get split down the middle in both the writing and reading code.
        // So that makes sure those code paths are tested.
        [TestCase("a🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔" +
                  "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔" +
                  "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔" +
                  "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔" +
                  "🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔🤔")]
        public void TestString(string? str)
        {
            var stream = new MemoryStream();
            Primitives.WritePrimitive(stream, str);
            stream.Position = 0;

            Primitives.ReadPrimitive(stream, out string? deserialized);
            Assert.That(deserialized, Is.EqualTo(str));
        }

        [Test]
        public void TestStringEndOfStream()
        {
            var stream = new MemoryStream();
            Primitives.WritePrimitive(stream, (uint)2000);
            Primitives.WritePrimitive(stream, (uint)1000);

            stream.Position = 0;
            Assert.That(() => Primitives.ReadPrimitive(stream, out string _), Throws.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void TestStringDestTooShort()
        {
            var stream = new MemoryStream();
            Primitives.WritePrimitive(stream, (uint)2000);
            Primitives.WritePrimitive(stream, (uint)5);
            Primitives.WritePrimitive(stream, new byte[2000]);

            stream.Position = 0;
            Assert.That(() => Primitives.ReadPrimitive(stream, out string _), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void TestImmutableDictionary()
        {
            var x = new Dictionary<string, int>
            {
                { "A", 1 },
                { "B", 2 }
            }.ToImmutableDictionary();

            var serializer = new Serializer([typeof(ImmutableDictionary<string, int>)], new Settings());
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, x);
            stream.Position = 0;
            serializer.DeserializeDirect(stream, out ImmutableDictionary<string, int> read);

            Assert.That(read, NUnit.Framework.Is.EquivalentTo(x));
        }

        [Test]
        public void TestImmutableHashSet()
        {
            var x = new HashSet<string> {"A", "B"}.ToImmutableHashSet();

            var serializer = new Serializer([typeof(ImmutableHashSet<string>)], new Settings());
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, x);
            stream.Position = 0;
            serializer.DeserializeDirect(stream, out ImmutableHashSet<string> read);

            Assert.That(read, NUnit.Framework.Is.EquivalentTo(x));
        }
    }
}
