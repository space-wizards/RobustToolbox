using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(MarkupNode))]
public sealed class MarkupNodeTest
{
    [Test]
    [SuppressMessage("Assertion", "NUnit2009:The same value has been provided as both the actual and the expected argument")]
    public void TestEquality()
    {
        Assert.Multiple(() =>
        {
            // Test name match
            Assert.That(new MarkupNode("A"), Is.EqualTo(new MarkupNode("A")));
            Assert.That(new MarkupNode("A").GetHashCode(), Is.EqualTo(new MarkupNode("A").GetHashCode()));
            Assert.That(new MarkupNode("A"), Is.Not.EqualTo(new MarkupNode("B")));

            // Test closing match
            Assert.That(new MarkupNode("A", null, null, true), Is.EqualTo(new MarkupNode("A", null, null, true)));
            Assert.That(new MarkupNode("A", null, null, true).GetHashCode(), Is.EqualTo(new MarkupNode("A", null, null, true).GetHashCode()));
            Assert.That(new MarkupNode("A", null, null, true), Is.Not.EqualTo(new MarkupNode("A")));

            // Test value match
            var param = new MarkupParameter("A");
            Assert.That(new MarkupNode("A", param, null), Is.EqualTo(new MarkupNode("A", param, null)));
            Assert.That(new MarkupNode("A", param, null).GetHashCode(), Is.EqualTo(new MarkupNode("A", param, null).GetHashCode()));
            Assert.That(new MarkupNode("A", param, null), Is.Not.EqualTo(new MarkupNode("A")));
            Assert.That(new MarkupNode("A", param, null), Is.Not.EqualTo(new MarkupNode("A", new MarkupParameter("B"), null)));

            // Test attributes match
            var attrs = new Dictionary<string, MarkupParameter>
            {
                { "A", new MarkupParameter("A") },
                { "B", new MarkupParameter(5) },
                { "C", new MarkupParameter(Color.Red) },
            };
            var wrongAttrs = new Dictionary<string, MarkupParameter>
            {
                { "A", new MarkupParameter("A") },
                { "B", new MarkupParameter(6) },
                { "C", new MarkupParameter(Color.Red) },
            };
            var wrongAttrsTooLong = new Dictionary<string, MarkupParameter>
            {
                { "A", new MarkupParameter("A") },
                { "B", new MarkupParameter(5) },
                { "C", new MarkupParameter(Color.Red) },
                { "D", new MarkupParameter(Color.Red) },
            };
            var attrs2 = attrs.Reverse().ToDictionary();
            Assert.That(new MarkupNode("A", null, attrs), Is.EqualTo(new MarkupNode("A", null, attrs2)));
            Assert.That(new MarkupNode("A", null, attrs).GetHashCode(), Is.EqualTo(new MarkupNode("A", null, attrs2).GetHashCode()));
            Assert.That(new MarkupNode("A", null, attrs), Is.Not.EqualTo(new MarkupNode("A")));
            Assert.That(new MarkupNode("A", null, attrs), Is.Not.EqualTo(new MarkupNode("A", null, wrongAttrs)));
            Assert.That(new MarkupNode("A", null, attrs), Is.Not.EqualTo(new MarkupNode("A", null, wrongAttrsTooLong)));
        });
    }
}
