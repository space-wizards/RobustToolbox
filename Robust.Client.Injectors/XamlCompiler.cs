using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Robust.Build.Tasks
{
    /// <summary>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/XamlCompilerTaskExecutor.cs
    /// Adjusted for our UI-Framework
    /// </summary>
    public partial class XamlCompiler
    {
        public static (bool success, bool writtentofile) Compile(IBuildEngine engine, string input, string[] references,
            string projectDirectory, string output, string strongNameKey)
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

            var xamlLanguage = new XamlLanguageTypeMappings(typeSystem)
            {
                XmlnsAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute"),

                },
                ContentAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.ContentAttribute")
                },
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.UsableDuringInitializationAttribute")
                },
                DeferredContentPropertyAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.DeferredContentAttribute")
                },
                RootObjectProvider = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestRootObjectProvider"),
                UriContextProvider = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestUriContext"),
                ProvideValueTarget = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestProvideValueTarget"),
            };
            var emitConfig = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
            {
                ContextTypeBuilderCallback = (b,c) => EmitNameScopeField(xamlLanguage, typeSystem, b, c)
            };

            var transformerconfig = new TransformerConfiguration(
                typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage), CustomValueConverter);

            var contextDef = new TypeDefinition("CompiledRobustXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);
            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler =
                new RobustXamlILCompiler(transformerconfig, emitConfig, true);

            var loaderDispatcherDef = new TypeDefinition("CompiledRobustXaml", "!XamlLoader",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);

            var loaderDispatcherMethod = new MethodDefinition("TryLoad",
                MethodAttributes.Static | MethodAttributes.Public,
                asm.MainModule.TypeSystem.Object)
            {
                Parameters = {new ParameterDefinition(asm.MainModule.TypeSystem.String)}
            };
            loaderDispatcherDef.Methods.Add(loaderDispatcherMethod);
            asm.MainModule.Types.Add(loaderDispatcherDef);

            var stringEquals = asm.MainModule.ImportReference(asm.MainModule.TypeSystem.String.Resolve().Methods.First(
                m =>
                    m.IsStatic && m.Name == "Equals" && m.Parameters.Count == 2 &&
                    m.ReturnType.FullName == "System.Boolean"
                    && m.Parameters[0].ParameterType.FullName == "System.String"
                    && m.Parameters[1].ParameterType.FullName == "System.String"));

#if DEBUG
            //Debugger.Launch();
            var hotReloadManager = asm.MainModule.ImportReference(typeSystem.GetTypeReference(typeSystem.FindType("Robust.Client.Debugging.XAMLUI.XamlUiHotreloadManager")));
            var hotReloadMethod = hotReloadManager.Resolve().Methods.First(m => m.Name == "TryHotReloading");

            var iocmanager = typeSystem.GetTypeReference(typeSystem.FindType("Robust.Shared.IoC.IoCManager")).Resolve();
            var resolveXamluiManagerMethod = iocmanager.Methods.First(m => m.Name == "Resolve");

            var resolveXamluiManagerMethodRef = asm.MainModule.ImportReference(resolveXamluiManagerMethod);
            resolveXamluiManagerMethodRef.ReturnType = hotReloadManager;
#endif

            bool CompileGroup(IResourceGroup group)
            {
                var typeDef = new TypeDefinition("CompiledRobustXaml", "!" + group.Name, TypeAttributes.Class,
                    asm.MainModule.TypeSystem.Object);

                //typeDef.CustomAttributes.Add(new CustomAttribute(ed));
                asm.MainModule.Types.Add(typeDef);
                var builder = typeSystem.CreateTypeBuilder(typeDef);

                foreach (var res in group.Resources.Where(CheckXamlName))
                {
                    try
                    {
                        engine.LogMessage($"XAMLIL: {res.Name} -> {res.Uri}", MessageImportance.Low);

                        var xaml = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                        var parsed = XDocumentXamlParser.Parse(xaml);

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

                        compiler.Transform(parsed);

                        var populateName = $"Populate:{res.Name}";
                        var buildName = $"Build:{res.Name}";

                        var classTypeDefinition = typeSystem.GetTypeReference(classType).Resolve();

                        var populateBuilder = typeSystem.CreateTypeBuilder(classTypeDefinition);

                        compiler.Compile(parsed, contextClass,
                            compiler.DefinePopulateMethod(populateBuilder, parsed, populateName,
                                classTypeDefinition == null),
                            compiler.DefineBuildMethod(builder, parsed, buildName, true),
                            null,
                            (closureName, closureBaseType) =>
                                populateBuilder.DefineSubType(closureBaseType, closureName, false),
                            res.Uri, res
                        );

                        //add compiled populate method
                        var compiledPopulateMethod = typeSystem.GetTypeReference(populateBuilder).Resolve().Methods
                            .First(m => m.Name == populateName);

                        const string TrampolineName = "!XamlIlPopulateTrampoline";
                        var trampoline = new MethodDefinition(TrampolineName,
                            MethodAttributes.Static | MethodAttributes.Private, asm.MainModule.TypeSystem.Void);
                        trampoline.Parameters.Add(new ParameterDefinition(classTypeDefinition));
                        classTypeDefinition.Methods.Add(trampoline);

                        var ret = Instruction.Create(OpCodes.Ret);
#if DEBUG
                        var typedHotReloadMethodRef = asm.MainModule.ImportReference(hotReloadMethod);
                        typedHotReloadMethodRef.Parameters.First().ParameterType = classTypeDefinition;
                        //typedHotReloadMethodRef = asm.MainModule.ImportReference(typedHotReloadMethodRef);
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, resolveXamluiManagerMethodRef));
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, typedHotReloadMethodRef));
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue_S, ret));
#endif

                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compiledPopulateMethod));
                        trampoline.Body.Instructions.Add(ret);

                        var foundXamlLoader = false;
                        // Find RobustXamlLoader.Load(this) and replace it with !XamlIlPopulateTrampoline(this)
                        foreach (var method in classTypeDefinition.Methods
                            .Where(m => !m.Attributes.HasFlag(MethodAttributes.Static)))
                        {
                            var i = method.Body.Instructions;
                            for (var c = 1; c < i.Count; c++)
                            {
                                if (i[c].OpCode == OpCodes.Call)
                                {
                                    var op = i[c].Operand as MethodReference;

                                    if (op != null
                                        && op.Name == TrampolineName)
                                    {
                                        foundXamlLoader = true;
                                        break;
                                    }

                                    if (op != null
                                        && op.Name == "Load"
                                        && op.Parameters.Count == 1
                                        && op.Parameters[0].ParameterType.FullName == "System.Object"
                                        && op.DeclaringType.FullName == "Robust.Client.UserInterface.XAML.RobustXamlLoader")
                                    {
                                        if (MatchThisCall(i, c - 1))
                                        {
                                            i[c].Operand = trampoline;
                                            foundXamlLoader = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (!foundXamlLoader)
                        {
                            var ctors = classTypeDefinition.GetConstructors()
                                .Where(c => !c.IsStatic).ToList();
                            // We can inject xaml loader into default constructor
                            if (ctors.Count == 1 && ctors[0].Body.Instructions.Count(o=>o.OpCode != OpCodes.Nop) == 3)
                            {
                                var i = ctors[0].Body.Instructions;
                                var retIdx = i.IndexOf(i.Last(x => x.OpCode == OpCodes.Ret));
                                i.Insert(retIdx, Instruction.Create(OpCodes.Call, trampoline));
                                i.Insert(retIdx, Instruction.Create(OpCodes.Ldarg_0));
                            }
                            else
                            {
                                throw new InvalidProgramException(
                                    $"No call to RobustXamlLoader.Load(this) call found anywhere in the type {classType.FullName} and type seems to have custom constructors.");
                            }
                        }

                        //add compiled build method
                        var compiledBuildMethod = typeSystem.GetTypeReference(builder).Resolve().Methods
                            .First(m => m.Name == buildName);
                        var parameterlessCtor = classTypeDefinition.GetConstructors()
                            .FirstOrDefault(c => c.IsPublic && !c.IsStatic && !c.HasParameters);

                        if (compiledBuildMethod != null && parameterlessCtor != null)
                        {
                            var i = loaderDispatcherMethod.Body.Instructions;
                            var nop = Instruction.Create(OpCodes.Nop);
                            i.Add(Instruction.Create(OpCodes.Ldarg_0));
                            i.Add(Instruction.Create(OpCodes.Ldstr, res.Uri));
                            i.Add(Instruction.Create(OpCodes.Call, stringEquals));
                            i.Add(Instruction.Create(OpCodes.Brfalse, nop));
                            if (parameterlessCtor != null)
                                i.Add(Instruction.Create(OpCodes.Newobj, parameterlessCtor));
                            else
                            {
                                i.Add(Instruction.Create(OpCodes.Call, compiledBuildMethod));
                            }

                            i.Add(Instruction.Create(OpCodes.Ret));
                            i.Add(nop);
                        }
                    }
                    catch (Exception e)
                    {
                        engine.LogWarningEvent(new BuildWarningEventArgs("XAMLIL", "", res.Uri, 0, 0, 0, 0,
                            e.ToString(), "", "CompileRobustXaml"));
                    }
                }
                return true;
            }

            if (embrsc.Resources.Count(CheckXamlName) != 0)
            {
                if (!CompileGroup(embrsc))
                    return false;
            }

            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            loaderDispatcherMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return true;
        }

        private static bool CustomValueConverter(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IXamlType type,
            out IXamlAstValueNode result)
        {
            if (!(node is XamlAstTextNode textNode))
            {
                result = null;
                return false;
            }

            var text = textNode.Text;
            var types = context.GetRobustTypes();

            if (type.Equals(types.Vector2))
            {
                var foo = MathParsing.Single2.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Vector2", node);

                var (x, y) = foo.Value;

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Vector2, types.Vector2ConstructorFull,
                    types.Single, new[] {x, y});
                return true;
            }

            if (type.Equals(types.Thickness))
            {
                var foo = MathParsing.Thickness.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Thickness", node);

                var val = foo.Value;
                float[] full;
                if (val.Length == 1)
                {
                    var u = val[0];
                    full = new[] {u, u, u, u};
                }
                else if (val.Length == 2)
                {
                    var h = val[0];
                    var v = val[1];
                    full = new[] {h, v, h, v};
                }
                else // 4
                {
                    full = val;
                }

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Thickness, types.ThicknessConstructorFull,
                    types.Single, full);
                return true;
            }

            if (type.Equals(types.Thickness))
            {
                var foo = MathParsing.Thickness.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Thickness", node);

                var val = foo.Value;
                float[] full;
                if (val.Length == 1)
                {
                    var u = val[0];
                    full = new[] {u, u, u, u};
                }
                else if (val.Length == 2)
                {
                    var h = val[0];
                    var v = val[1];
                    full = new[] {h, v, h, v};
                }
                else // 4
                {
                    full = val;
                }

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Thickness, types.ThicknessConstructorFull,
                    types.Single, full);
                return true;
            }

            if (type.Equals(types.Color))
            {
                // TODO: Interpret these colors at XAML compile time instead of at runtime.
                result = new RXamlColorAstNode(node, types, text);
                return true;
            }

            result = null;
            return false;
        }

        public const string ContextNameScopeFieldName = "RobustNameScope";

        private static void EmitNameScopeField(XamlLanguageTypeMappings xamlLanguage, CecilTypeSystem typeSystem, IXamlTypeBuilder<IXamlILEmitter> typeBuilder, IXamlILEmitter constructor)
        {
            var nameScopeType = typeSystem.FindType("Robust.Client.UserInterface.XAML.NameScope");
            var field = typeBuilder.DefineField(nameScopeType,
                ContextNameScopeFieldName, true, false);
            constructor
                .Ldarg_0()
                .Newobj(nameScopeType.GetConstructor())
                .Stfld(field);
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
