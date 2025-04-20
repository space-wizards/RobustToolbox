using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using VerifyCS =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.DataDefinitionAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class DataDefinitionAnalyzerTest
{
    private static Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DataDefinitionAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { code }
            },
        };

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    [Test]
    public async Task Test()
    {
        const string code = """
            using System;
            using Robust.Shared.ViewVariables;
            using Robust.Shared.Serialization.Manager.Attributes;

            namespace Robust.Shared.ViewVariables
            {
                public sealed class ViewVariablesAttribute : Attribute
                {
                    public readonly VVAccess Access = VVAccess.ReadOnly;

                    public ViewVariablesAttribute() { }

                    public ViewVariablesAttribute(VVAccess access)
                    {
                        Access = access;
                    }
                }
                public enum VVAccess : byte
                {
                    ReadOnly = 0,
                    ReadWrite = 1,
                }
            }

            namespace Robust.Shared.Serialization.Manager.Attributes
            {
                public class DataFieldBaseAttribute : Attribute;
                public class DataFieldAttribute : DataFieldBaseAttribute;
                public sealed class DataDefinitionAttribute : Attribute;
            }

            [DataDefinition]
            public sealed partial class Foo
            {
                [DataField, ViewVariables(VVAccess.ReadWrite)]
                public int Bad;

                [DataField]
                public int Good;

                [DataField, ViewVariables]
                public int Good2;

                [DataField, ViewVariables(VVAccess.ReadOnly)]
                public int Good3;

                [ViewVariables]
                public int Good4;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(35,17): info RA0028: Data field Bad in data definition Foo has ViewVariables attribute with ReadWrite access, which is redundant
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldNoVVReadWriteRule).WithSpan(35, 17, 35, 50).WithArguments("Bad", "Foo")
        );
    }

    [Test]
    public async Task ReadOnlyFieldTest()
    {
        const string code = """
            using System;
            using Robust.Shared.Serialization.Manager.Attributes;

            namespace Robust.Shared.Serialization.Manager.Attributes
            {
                public class DataFieldBaseAttribute : Attribute;
                public class DataFieldAttribute : DataFieldBaseAttribute;
                public sealed class DataDefinitionAttribute : Attribute;
            }

            [DataDefinition]
            public sealed partial class Foo
            {
                [DataField]
                public readonly int Bad;

                [DataField]
                public int Good;
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(15,12): error RA0019: Data field Bad in data definition Foo is readonly
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldWritableRule).WithSpan(15, 12, 15, 20).WithArguments("Bad", "Foo")
        );
    }

    [Test]
    public async Task ReadOnlyPropertyTest()
    {
        const string code = """
            using System;
            using Robust.Shared.Serialization.Manager.Attributes;

            namespace Robust.Shared.Serialization.Manager.Attributes
            {
                public class DataFieldBaseAttribute : Attribute;
                public class DataFieldAttribute : DataFieldBaseAttribute;
                public sealed class DataDefinitionAttribute : Attribute;
            }

            [DataDefinition]
            public sealed partial class Foo
            {
                [DataField]
                public int Bad { get; }

                [DataField]
                public int Good { get; private set; }
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(15,20): error RA0020: Data field property Bad in data definition Foo does not have a setter
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldPropertyWritableRule).WithSpan(15, 20, 15, 28).WithArguments("Bad", "Foo")
        );
    }
}
