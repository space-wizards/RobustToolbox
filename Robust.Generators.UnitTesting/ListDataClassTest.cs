using NUnit.Framework;

namespace Robust.Generators.UnitTesting
{
    public class ListDataClassTest : AnalyzerTest
    {
        [Test]
        public void ListNoDCTest()
        {
            const string source = @"
using Robust.Shared.Prototypes.DataClasses.Attributes;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Test{
    [DataClass]
    public class TestClass{
        [YamlField(""myList"")]
        public List<string> testList;
        [YamlField(""myList"")]
        public string abc;
    }
}
";
            var comp = CreateCompilation(source);

            Assert.IsEmpty(comp.GetDiagnostics());

            var (newcomp, generatorDiags) = RunGenerators(comp, new DataClassGenerator());

            Assert.IsEmpty(generatorDiags);

            //TODO Check for dataclass & if its correct
        }
    }
}
