using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Client.XAMLUI
{
    [Generator]
    public class TestSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //nix
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var source = @"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello()
        {
            Console.WriteLine(""Hello from generated code!"");
        }
    }
}";
            context.AddSource("helloWorldGen", SourceText.From(source, Encoding.UTF8));
        }
    }
}
