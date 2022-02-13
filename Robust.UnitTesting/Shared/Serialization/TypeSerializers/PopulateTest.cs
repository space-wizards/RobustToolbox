using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    public sealed class PopulateNullableStructTest : SerializationTest
    {
        [DataDefinition]
        private struct TestStruct : IPopulateDefaultValues
        {
            public bool Populated { get; set; }

            public void PopulateDefaultValues()
            {
                Populated = true;
            }
        }

        [Test]
        public void PopulateStruct()
        {
            var value = Serialization.ReadValue<TestStruct>(new ValueDataNode(string.Empty));

            Assert.True(value.Populated);
        }

        [Test]
        public void PopulateNullableStruct()
        {
            var value = Serialization.ReadValue<TestStruct?>(new ValueDataNode(string.Empty));

            Assert.NotNull(value);
            Assert.True(value.HasValue);
            Assert.True(value!.Value.Populated);
        }
    }
}
