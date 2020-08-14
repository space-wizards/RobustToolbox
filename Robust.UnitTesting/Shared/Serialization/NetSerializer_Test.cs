using System;
using System.Collections.Generic;
using System.IO;
using NetSerializer;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Serialization
{
    // Tests NetSerializer itself because we have specific modifications.
    // e.g. (at the time of writing) list serialization being more compact.
    [Parallelizable(ParallelScope.All)]
    public class NetSerializer_Test
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
                Assert.Null(deserialized);
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
                Assert.Null(deserialized);
            }
            else
            {
                Assert.That(deserialized, Is.EquivalentTo(list));
            }
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
                Assert.Null(deserialized);
            }
            else
            {
                Assert.That(deserialized, Is.EquivalentTo(set));
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
    }
}
