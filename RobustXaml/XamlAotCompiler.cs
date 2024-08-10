using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Pidgin;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace RobustXaml
{
    /// <summary>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/XamlCompilerTaskExecutor.cs
    /// Adjusted for our UI-Framework
    /// </summary>
    public partial class XamlAotCompiler
    {
        public static (bool success, bool writtentofile) Compile(IBuildEngine engine, string input, string[] references,
            string projectDirectory, string output, string? strongNameKey)
        {
            var typeSystem = new CecilTypeSystem(references
                .Where(r => !r.ToLowerInvariant().EndsWith("robust.build.tasks.dll"))
                .Concat(new[] { input }), input);

            var asm = typeSystem.TargetAssemblyDefinition;

            if (asm.MainModule.GetType("CompiledRobustXaml", "XamlIlContext") != null)
            {
                // If this type exists, the assembly has already been processed by us.
                // Do not run again, it would corrupt the file.
                // This *shouldn't* be possible due to Inputs/Outputs dependencies in the build system,
                // but better safe than sorry eh?
                engine.LogWarningEvent(new BuildWarningEventArgs("XAMLIL", "", "", 0, 0, 0, 0, "Ran twice on same assembly file; ignoring.", "", ""));
                return (true, false);
            }

            var compileRes = CompileCore(engine, typeSystem);
            if (compileRes == null)
                return (true, false);
            if (compileRes == false)
                return (false, false);

            var writerParameters = new WriterParameters { WriteSymbols = asm.MainModule.HasSymbols };
            if (!string.IsNullOrWhiteSpace(strongNameKey))
                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);

            asm.Write(output, writerParameters);

            return (true, true);

        }

        static bool? CompileCore(IBuildEngine engine, CecilTypeSystem typeSystem)
        {
            var asm = typeSystem.TargetAssemblyDefinition;
            var embrsc = new EmbeddedResources(asm);

            if (embrsc.Resources.Count(CheckXamlName) == 0)
                // Nothing to do
                return null;

            var xaml = new XamlCustomizations(typeSystem, typeSystem.TargetAssembly);
            var lowLevel = new LowLevelCustomizations(typeSystem);

            var contextDef = new TypeDefinition("CompiledRobustXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);
            var contextClass = XamlILContextDefinition.GenerateContextClass(
                typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xaml.TypeMappings, xaml.EmitMappings
            );

            bool CompileGroup(IResourceGroup group)
            {
                var typeDef = new TypeDefinition("CompiledRobustXaml", "!" + group.Name, TypeAttributes.Class,
                    asm.MainModule.TypeSystem.Object);

                asm.MainModule.Types.Add(typeDef);

                foreach (var res in group.Resources.Where(CheckXamlName))
                {
                    try
                    {
                        engine.LogMessage($"XAMLIL: {res.Name} -> {res.Uri}", MessageImportance.Low);

                        var xamlText = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                        var parsed = XDocumentXamlParser.Parse(xamlText);

                        var initialRoot = (XamlAstObjectNode) parsed.Root;

                        var classDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                            .FirstOrDefault(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class");
                        string classname;
                        if (classDirective != null && classDirective.Values[0] is XamlAstTextNode tn)
                        {
                            classname = tn.Text;
                        }
                        else
                        {
                            classname = res.Name.Replace(".xaml","");
                        }

                        var classType = typeSystem.TargetAssembly.FindType(classname);
                        if (classType == null)
                            throw new Exception($"Unable to find type '{classname}'");

                        xaml.ILCompiler.Transform(parsed);

                        var populateName = $"Populate:{res.Name}";

                        var classTypeDefinition = typeSystem.GetTypeReference(classType).Resolve()!;
                        var populateBuilder = typeSystem.CreateTypeBuilder(classTypeDefinition);

                        xaml.ILCompiler.Compile(parsed, contextClass,
                            xaml.ILCompiler.DefinePopulateMethod(populateBuilder, parsed, populateName, true),
                            null,
                            null,
                            (closureName, closureBaseType) =>
                                populateBuilder.DefineSubType(closureBaseType, closureName, false),
                            res.Uri, res
                        );

                        var compiledPopulateMethod = typeSystem.GetTypeReference(populateBuilder).Resolve().Methods
                            .First(m => m.Name == populateName);

                        lowLevel.AddXamlMetadata(classTypeDefinition, new Uri(res.Uri), res.FilePath, xamlText);
                        var foundXamlLoader = lowLevel.TrampolineCallsToXamlLoader(classTypeDefinition, compiledPopulateMethod);

                        if (!foundXamlLoader)
                        {
                            throw new InvalidProgramException(
                                $"No call to RobustXamlLoader.Load(this) call found anywhere in the type {classType.FullName} and type seems to have custom constructors.");
                        }
                    }
                    catch (Exception e)
                    {
                        engine.LogErrorEvent(new BuildErrorEventArgs("XAMLIL", "", res.FilePath, 0, 0, 0, 0,
                            $"{res.FilePath}: {e.Message}", "", "CompileRobustXaml"));
                    }
                    res.Remove();
                }
                return true;
            }

            if (embrsc.Resources.Count(CheckXamlName) != 0)
            {
                if (!CompileGroup(embrsc))
                    return false;
            }

            return true;
        }
    }

    interface IResource : IFileSource
    {
        string Uri { get; }
        string Name { get; }
        void Remove();

    }

    interface IResourceGroup
    {
        string Name { get; }
        IEnumerable<IResource> Resources { get; }
    }
}
