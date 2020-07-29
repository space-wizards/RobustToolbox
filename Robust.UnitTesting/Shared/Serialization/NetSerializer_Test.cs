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
    }
}
