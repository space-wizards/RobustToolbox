using NUnit.Framework;

namespace Robust.Generators.UnitTesting
{
    public class DataClassTests : AnalyzerTest
    {
        [Test]
        public void DCTest()
        {
            const string source = @"
using Robust.Shared.Prototypes.DataClasses.Attributes;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
//using Robust.Shared.Serialization;

namespace Test{
        public class TestClassData {
            //public override void ExposeData(ObjectSerializer serializer)
            //{}
        }

        [DataClass(typeof(TestClassData))]
        public class TestClass{
        [YamlField(""myList"")]
        public List<TestClass> testList;
        [YamlField(""myList"")]
        public string abc = ""testing"";
        [YamlField(""drawdepth"", constType: typeof(TestClass))]
        public int test;
    }
}
";
            var comp = CreateCompilation(source);

            Assert.IsEmpty(comp.GetDiagnostics());

            var (newcomp, generatorDiags) = RunGenerators(comp, new DataClassGenerator());

            Assert.IsEmpty(generatorDiags);
            Assert.NotNull(newcomp.GetTypeByMetadataName("Test.TestClass_AUTODATA"));

            //TODO Check for dataclass & if its correct
        }
    }
}
