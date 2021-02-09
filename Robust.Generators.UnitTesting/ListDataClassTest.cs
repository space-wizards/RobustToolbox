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
using System;

namespace Test{
    [Serializable]
    public class Thingy {}

    public class TestClass{
        [YamlField(""myList"")]
        public List<string> testList;
        [YamlField(""myList"")]
        public Hashset<string> testList;
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
