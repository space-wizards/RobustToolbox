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

        test.TestState.Sources.Add(("TestTypeDefs.cs", TestTypeDefs));

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    private const string TestTypeDefs = """
        using System;

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
            public sealed class NotYamlSerializableAttribute : Attribute;
        }
    """;

    [Test]
    public async Task NoVVReadOnlyTest()
    {
        const string code = """
            using Robust.Shared.ViewVariables;
            using Robust.Shared.Serialization.Manager.Attributes;

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
            // /0/Test0.cs(7,17): info RA0028: Data field Bad in data definition Foo has ViewVariables attribute with ReadWrite access, which is redundant
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldNoVVReadWriteRule).WithSpan(7, 17, 7, 50).WithArguments("Bad", "Foo")
        );
    }

    [Test]
    public async Task ReadOnlyFieldTest()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

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
            // /0/Test0.cs(7,12): error RA0019: Data field Bad in data definition Foo is readonly
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldWritableRule).WithSpan(7, 12, 7, 20).WithArguments("Bad", "Foo")
        );
    }

    [Test]
    public async Task ReadOnlyPropertyTest()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

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
            // /0/Test0.cs(7,20): error RA0020: Data field property Bad in data definition Foo does not have a setter
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldPropertyWritableRule).WithSpan(7, 20, 7, 28).WithArguments("Bad", "Foo")
        );
    }

    [Test]
    public async Task NotYamlSerializableTest()
    {
        const string code = """
            using Robust.Shared.Serialization.Manager.Attributes;

            [NotYamlSerializable]
            public sealed class NotSerializableClass { }

            [DataDefinition]
            public sealed partial class Foo
            {
                [DataField]
                public NotSerializableClass BadField;

                [DataField]
                public NotSerializableClass BadProperty { get; set; }

                public NotSerializableClass GoodField; // Not a DataField, not a problem

                public NotSerializableClass GoodProperty { get; set; } // Not a DataField, not a problem
            }
            """;

        await Verifier(code,
            // /0/Test0.cs(10,12): error RA0033: Data field BadField in data definition Foo is type NotSerializableClass, which is not YAML serializable
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldYamlSerializableRule).WithSpan(10, 12, 10, 32).WithArguments("BadField", "Foo", "NotSerializableClass"),
            // /0/Test0.cs(13,12): error RA0033: Data field BadProperty in data definition Foo is type NotSerializableClass, which is not YAML serializable
            VerifyCS.Diagnostic(DataDefinitionAnalyzer.DataFieldYamlSerializableRule).WithSpan(13, 12, 13, 32).WithArguments("BadProperty", "Foo", "NotSerializableClass")
        );
    }
}
