using NUnit.Framework;

namespace Robust.Generators.UnitTesting
{
    public class ListDataClassTest : AnalyzerTest
    {
        [Test]
        public void ListNoDCTest()
        {
            const string source = @"
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Test{
    public class TestClass{
        [YamlField(""myList"")]
        public List<string> testList;
    }
}
";
            var comp = CreateCompilation(source);
            var (newcomp, generatorDiags) = RunGenerators(comp, new DataClassGenerator());

            Assert.IsEmpty(generatorDiags);

            //TODO Check for dataclass & if its correct
        }
    }
}
