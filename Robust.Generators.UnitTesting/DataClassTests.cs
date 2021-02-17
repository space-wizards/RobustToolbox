using System.Linq;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

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

            var type = newcomp.GetTypeByMetadataName("Test.TestClass_AUTODATA");
            Assert.NotNull(type);

            var memberNames = type.MemberNames.ToArray();

            // 3 properties
            Assert.That(memberNames, Has.Length.EqualTo(3));
            Assert.That(memberNames, Contains.Item("testList"));
            Assert.That(memberNames, Contains.Item("abc"));
            Assert.That(memberNames, Contains.Item("test"));

            var members = type.GetMembers();

            // 3 properties + constructor
            Assert.That(members, Has.Length.EqualTo(4));

            var memberDictionary = members.ToDictionary(m => m.Name, m => m);
            var yamlFieldNamespace = typeof(YamlFieldAttribute).FullName;

            Assert.NotNull(yamlFieldNamespace);

            var yamlFieldAttribute = comp.GetTypeByMetadataName(yamlFieldNamespace);

            var testListYamlAttribute = memberDictionary["testList"].GetAttribute(yamlFieldAttribute);
            Assert.NotNull(testListYamlAttribute);
            Assert.That(testListYamlAttribute.ConstructorArguments[0].Value, Is.EqualTo("myList"));

            var abcYamlAttribute = memberDictionary["abc"].GetAttribute(yamlFieldAttribute);
            Assert.NotNull(abcYamlAttribute);
            Assert.That(abcYamlAttribute.ConstructorArguments[0].Value, Is.EqualTo("myList"));

            var testYamlAttribute = memberDictionary["test"].GetAttribute(yamlFieldAttribute);
            Assert.NotNull(testYamlAttribute);
            Assert.That(testYamlAttribute.ConstructorArguments[0].Value, Is.EqualTo("drawdepth"));
            Assert.NotNull(testYamlAttribute.ConstructorArguments[3].Value);
            Assert.That(testYamlAttribute.ConstructorArguments[3].Value.ToString(), Is.EqualTo("Test.TestClass"));

            //TODO Check for dataclass & if its correct
        }
    }
}
