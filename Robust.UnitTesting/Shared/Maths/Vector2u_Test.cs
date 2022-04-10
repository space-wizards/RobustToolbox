using System.Text.Json;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [Parallelizable]
    [TestOf(typeof(Vector2u))]
    public sealed class Vector2u_Test
    {
        // This test basically only exists because RSI loading needs it.
        [Test]
        public void TestJsonDeserialization()
        {
            Assert.That(
                JsonSerializer.Deserialize<Vector2u>("{\"x\": 10, \"y\": 10}",
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)), Is.EqualTo(new Vector2u(10, 10)));
        }
    }
}
