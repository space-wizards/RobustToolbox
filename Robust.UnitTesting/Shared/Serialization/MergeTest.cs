using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    public sealed class MergeTest : SerializationTest
    {
        [Test]
        public void Test()
        {
            var defA = new DataDefA();
            defA.Defs["foo"] = new DataDefB { A = 5, B = 10 };

            const string yaml = @"
defs:
    foo:
        b: 20
";

            var deserialization = Serialization.Read(typeof(DataDefA), YamlNodeHelpers.StringToDataNode(yaml));
            object defAObj = defA;
            Serialization.MergePopulate(ref defAObj, deserialization);
            defA = (DataDefA)defAObj;

            Assert.That(defA.Defs["foo"].A, Is.EqualTo(5));
            Assert.That(defA.Defs["foo"].B, Is.EqualTo(20));
        }

        [DataDefinition]
        public sealed class DataDefA
        {
            [DataField("defs")]
            public Dictionary<string, DataDefB> Defs { get; set; } = new();
        }

        [DataDefinition]
        public sealed class DataDefB
        {
            [DataField("a")]
            public int A { get; set; }
            [DataField("b")]
            public int B { get; set; }
        }
    }
}
