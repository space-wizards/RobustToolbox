using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XamlX.IL;
using XamlX.Parsers;

namespace Robust.Client.XamlUI
{
    [Generator]
    public class XamlUiPartialClassGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var additionalFile in context.AdditionalFiles)
            {
                if(!additionalFile.Path.EndsWith(".xaml")) continue;

                var txt = additionalFile.GetText()?.ToString();
                if(txt == null) continue; //TODO maybe log something here, i dunno

                var parsed = XDocumentXamlParser.Parse(txt);
                var t = new XamlILCompiler();
                t.Compile();
            }

            // begin creating the source we'll inject into the users compilation
            StringBuilder sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
");
            foreach (var additionalFile in context.AdditionalFiles)
            {
                if (additionalFile.Path.EndsWith(".xaml"))
                {
                    var lines = additionalFile.GetText()?.Lines;
                    foreach (var line in lines)
                    {
                        var txt = line.ToString();
                        if(txt.Length != 0)
                            sourceBuilder.AppendLine($@"public static void {txt}(){{ Console.WriteLine(""yes"");}}");
                    }
                }
            }

            // finish creating the source to inject
            sourceBuilder.Append(@"
    }
}");

            // inject the created source into the users compilation
            context.AddSource("helloWorldGenerated", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
            // Debugger.Launch();
        }
    }
}
