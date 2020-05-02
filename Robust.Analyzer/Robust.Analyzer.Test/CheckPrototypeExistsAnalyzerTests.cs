using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

using Robust.Analyzer;

namespace Robust.Analyzer.Test
{
    [TestClass]
    public class UnitTest : DiagnosticVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestEmpty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
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

            public static void DoSomeGameStuff()
            {
                IEntityManager entityManager = null;
                _ = entityManager.SpawnEntity(""some_entity"", MapCoordinates.Nullspace);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "RTE001",
                Message = "Could not find entity prototype with name '\"some_entity\"'.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 17)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }


        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CheckPrototypesExistAnalyzer();
        }
    }
}
