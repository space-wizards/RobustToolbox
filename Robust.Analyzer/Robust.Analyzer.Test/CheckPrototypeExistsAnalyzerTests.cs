using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.RepresentationModel;

using Robust.Analyzer;
using Robust.Shared.Prototypes;

namespace Robust.Analyzer.Test
{
    [Prototype("foobar")]
    public class FooBar : IPrototype, IIndexedPrototype
    {
        public void LoadFrom(YamlMappingNode _mapping) {}

        public string ID => "some_foobar";
    }

    [TestClass]
    public class UnitTest : DiagnosticVerifier
    {
        private static readonly string _prototypesYaml = @"
- type: entity
  id: some_entity";

        private static readonly string _foobarPrototypesYaml = @"
- type: entity
  id: some_entity
- type: foobar
  id: some_foobar";

        //No diagnostics expected to show up
        [TestMethod]
        public void TestPresentPrototype()
        {
            var test = @"
using System;

using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace SomeGameCode
{
    class SomeGameCodeClass
    {
        public static void Main() {}

        public static void DoSomeGameStuff()
        {
            IEntityManager entityManager = null;
            _ = entityManager.SpawnEntity(""some_entity"", MapCoordinates.Nullspace);
        }
    }
}";

            VerifyCSharpDiagnostic(new[] { test }, new[] { ("prototypes.yaml", _prototypesYaml) });
        }

        //Diagnostic should trigger
        [TestMethod]
        public void TestMissingPrototype()
        {
            var test = @"
using System;

using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace SomeGameCode
{
    class SomeGameCodeClass
    {
        public static void Main() {}

        public static void DoSomeGameStuff()
        {
            IEntityManager entityManager = null;
            _ = entityManager.SpawnEntity(""some_other_entity"", MapCoordinates.Nullspace);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "RTE001",
                Message = "Could not find 'entity' prototype with name 'some_other_entity'.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 17)
                        }
            };

            VerifyCSharpDiagnostic(new[] { test }, new[] { ("prototypes.yaml", _prototypesYaml) }, expected);
        }

        [TestMethod]
        public void TestMissingPrototypeWithOtherId()
        {
            var test = @"
using System;

using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace SomeGameCode
{
    class SomeGameCodeClass
    {
        public static void Main() {}

        public static void DoSomeGameStuff()
        {
            IEntityManager entityManager = null;
            _ = entityManager.SpawnEntity(""some_entity"", MapCoordinates.Nullspace);
            _ = entityManager.SpawnEntity(""some_foobar"", MapCoordinates.Nullspace);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "RTE001",
                Message = "Could not find 'entity' prototype with name 'some_foobar'.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 17)
                        }
            };

            VerifyCSharpDiagnostic(new[] { test }, new[] { ("prototypes.yaml", _foobarPrototypesYaml) }, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CheckPrototypesExistAnalyzer();
        }
    }
}
