using NUnit.Framework;
using Pidgin;
using Robust.Client.Graphics.Shaders;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.Graphics
{
    [TestOf(typeof(GlslParser))]
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class GlslParserTest
    {
        [Test]
        public void Test()
        {
            var parsed = GlslParser.ParserVec4.ParseOrThrow("vec4(1.0,0.0,0.0,0.33)");

            Assert.That(parsed, Is.EqualTo(new Vector4(1, 0, 0, 0.33f)));
        }
    }
}
