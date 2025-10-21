using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Mono.Cecil;
using XamlX;
using XamlX.Ast;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.TypeSystem;

namespace Robust.Xaml;

/// <summary>
/// Utility class: holds scope information for a Microsoft.Build.Framework
/// build in order to AOT-compile the XAML resources for an assembly.
/// </summary>
/// <remarks>
/// Also embed enough information to support future JIT attempts on those same resources.
///
/// Code primarily by Paul Ritter, touched by Pyrex in 2024.
///
/// Based on https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/XamlCompilerTaskExecutor.cs
/// Adjusted for our UI Framework
/// </remarks>
internal partial class XamlAotCompiler
{
    /// <summary>
    /// Update the assembly whose name is <paramref name="input" />, then
    /// save an updated assembly to <paramref name="output"/>.
    /// </summary>
    /// <param name="engine">the Microsoft build engine (used for logging)</param>
    /// <param name="input">the input assembly by name</param>
    /// <param name="references">all the assemblies that the input Xaml is allowed to reference</param>
    /// <param name="output">the place to put the output assembly</param>
    /// <param name="strongNameKey">
    ///   a file to use in order to generate a "strong name" for the assembly
    ///   (https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named)
    /// </param>
    /// <returns>
    ///     true if this succeeds and
    ///     true if the result was written to <paramref name="output"/>
    /// </returns>
    public static (bool success, bool writtentofile) Compile(IBuildEngine engine, string input, string[] references,
        string output, string? strongNameKey)
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
        if (!compileRes)
            return (false, false);

        var writerParameters = new WriterParameters { WriteSymbols = asm.MainModule.HasSymbols };
        if (!string.IsNullOrWhiteSpace(strongNameKey))
            writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);

        asm.Write(output, writerParameters);

        return (true, true);

    }

    /// <summary>
    /// For each XAML resource, identify its affiliated class, invoke the
    /// AOT compiler, update the class to call into the generated code,
    /// and write down metadata for future JIT compiles.
    /// </summary>
    /// <param name="engine">the Microsoft build engine (for logging)</param>
    /// <param name="typeSystem">the type system (which includes info about the target assembly)</param>
    /// <returns>true if compilation succeeded in every case</returns>
    static bool CompileCore(IBuildEngine engine, CecilTypeSystem typeSystem)
    {
        var asm = typeSystem.TargetAssemblyDefinition;
        var embrsc = new EmbeddedResources(asm);

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
                        throw new InvalidProgramException($"Unable to find type '{classname}'");

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
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// This is <see cref="IFileSource"/> from XamlX, augmented with the other
/// arguments that the XAML compiler wants.
/// </summary>
/// <remarks>
/// We store these later in the build process inside a XamlMetadataAttribute,
/// in order to support JIT compilation.
/// </remarks>
interface IResource : IFileSource
{
    string Uri { get; }
    string Name { get; }
    void Remove();

}

/// <summary>
/// A named collection of <see cref="IResource"/>s.
/// </summary>
interface IResourceGroup
{
    string Name { get; }
    IEnumerable<IResource> Resources { get; }
}
