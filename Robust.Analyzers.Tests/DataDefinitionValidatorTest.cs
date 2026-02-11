extern alias SerializationGenerator;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SerializationGenerator::Robust.Serialization.Generator;

namespace Robust.Analyzers.Tests;

[TestFixture]
[TestOf(typeof(Generator))]
[Parallelizable(ParallelScope.All)]
public sealed class DataDefinitionValidatorTest
{
    private const string TypesCode = """
    namespace Robust.Shared.GameObjects
    {
        [ImplicitDataDefinitionForInheritors]
        public interface IComponent;
    }

    namespace Robust.Shared.Serialization.Manager.Attributes
    {
        public sealed class ImplicitDataDefinitionForInheritorsAttribute : Attribute;
    }

    namespace Robust.Shared.Serialization.Manager.Attributes
    {
        public abstract class DataFieldBaseAttribute : Attribute
        {
            public readonly int Priority;
            public readonly Type? CustomTypeSerializer;
            public readonly bool ReadOnly;
            public readonly bool ServerOnly;

            protected DataFieldBaseAttribute(bool readOnly = false, int priority = 1, bool serverOnly = false, Type? customTypeSerializer = null)
            {
                ReadOnly = readOnly;
                Priority = priority;
                ServerOnly = serverOnly;
                CustomTypeSerializer = customTypeSerializer;
            }
        }

        public class DataFieldAttribute : DataFieldBaseAttribute
        {
            public string? Tag { get; internal set; }
            public readonly bool Required;

            public DataFieldAttribute(string? tag = null, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false, Type? customTypeSerializer = null) : base(readOnly, priority, serverOnly, customTypeSerializer)
            {
                Tag = tag;
                Required = required;
            }
        }
    }
    """;

    [Test]
    public void TestBasic()
    {
        var result = RunGenerator("""
            global using System;
            global using Robust.Shared.Analyzers;
            global using Robust.Shared.GameObjects;
            global using Robust.Shared.Serialization.Manager.Attributes;
            global using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

            public sealed partial class TestComponent : IComponent
            {
                [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
                public TimeSpan? NullableValueTypeCustom;
            }
            """);

        ExpectNoDiagnostics(result);
    }

    private static void ExpectNoDiagnostics(GeneratorRunResult result)
    {
        Assert.That(result.Diagnostics, Is.Empty);
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        var compilation = (Compilation)CSharpCompilation.Create("compilation",
            new[]
            {
                CSharpSyntaxTree.ParseText(source, path: "Source.cs"),
                CSharpSyntaxTree.ParseText(TypesCode, path: "Types.cs")
            },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Generator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        var result = driver.GetRunResult();

        return result.Results[0];
    }
}
