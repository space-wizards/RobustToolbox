using Newtonsoft.Json;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture]
    [Parallelizable]
    [TestOf(typeof(Vector2u))]
    public class Vector2u_Test
    {
        [Test]
        public void TestJsonDeserialization()
        {
            Assert.That(JsonConvert.DeserializeObject<Vector2u>("{\"x\": 10, \"y\": 10}"), Is.EqualTo(new Vector2u(10, 10)));
        }
    }
}
