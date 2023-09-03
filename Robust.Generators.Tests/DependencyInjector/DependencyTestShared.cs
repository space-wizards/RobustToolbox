using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Robust.Generators.Tests.DependencyInjector;

public static class DependencyTestShared
{
    public static SyntaxTree TypeDefinitionsSyntax { get; } = CSharpSyntaxTree.ParseText("""
        using System;

        namespace Robust.Shared.IoC;

        [AttributeUsage(AttributeTargets.Field)]
        public sealed class DependencyAttribute : System.Attribute
        {
        }

        public interface IDependencyInjector
        {
            Type[] ReportDependencies();
            void InjectDependencies(ReadOnlySpan<object> dependencies);
        }
        """);
}
