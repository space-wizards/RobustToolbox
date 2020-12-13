using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Robust.Build.Tasks
{
    public partial class XamlCompiler
    {
        public static (bool success, bool writtentofile) Compile(IBuildEngine engine, string input, string[] references,
            string projectDirectory, string output, string strongNameKey)//, bool patchCom)
        {
            var typeSystem = new CecilTypeSystem(references
                .Where(r => !r.ToLowerInvariant().EndsWith("robust.build.tasks.dll"))
                .Concat(new[] { input }), input);

            var asm = typeSystem.TargetAssemblyDefinition;

            var compileRes = CompileCore(engine, typeSystem, projectDirectory);
            if (compileRes == null) // && !patchCom)
                return (true, false);
            if (compileRes == false)
                return (false, false);

            //if (patchCom)
                //ComInteropHelper.PatchAssembly(asm, typeSystem);

            var writerParameters = new WriterParameters { WriteSymbols = asm.MainModule.HasSymbols };
            if (!string.IsNullOrWhiteSpace(strongNameKey))
                writerParameters.StrongNameKeyBlob = File.ReadAllBytes(strongNameKey);

            asm.Write(output, writerParameters);

            return (true, true);

        }

        static bool? CompileCore(IBuildEngine engine, CecilTypeSystem typeSystem,
            string projectDirectory)
        {
            var asm = typeSystem.TargetAssemblyDefinition;
            //todo get all xaml resources
            var all_xaml_resources = new List<IResource>();
            var embrsc = new EmbeddedResources(asm);

            if (all_xaml_resources.Count == 0)
                // Nothing to do
                return null;

            var xamlLanguage = new XamlLanguageTypeMappings(typeSystem)
            {
                XmlnsAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.XmlnsDefinitionAttribute"),

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
                ProvideValueTargetPropertyEmitter = null, //TODO
                //TODO something with namescopes ContextTypeBuilderCallback = (b,c) => Emitnamescopefield(xamlLanguage, typeSystem, b, c)
            };

            var transformerconfig = new TransformerConfiguration(
                typeSystem,
                typeSystem.TargetAssembly,
                xamlLanguage,
                XamlXmlnsMappings.Resolve(typeSystem, xamlLanguage));

            var contextDef = new TypeDefinition("CompiledRobustXaml", "XamlIlContext",
                TypeAttributes.Class, asm.MainModule.TypeSystem.Object);
            asm.MainModule.Types.Add(contextDef);
            var contextClass = XamlILContextDefinition.GenerateContextClass(typeSystem.CreateTypeBuilder(contextDef), typeSystem,
                xamlLanguage, emitConfig);

            var compiler =
                new XamlILCompiler(transformerconfig, emitConfig,
                    true); //compilerConfig, emitConfig, contextClass) { EnableIlVerification = verifyIl };

            bool CompileForXamlFile(IResource res)
            {
                engine.LogMessage($"XAMLIL: {res.Name} -> {res.Uri}", MessageImportance.Low);

                var xaml = new StreamReader(new MemoryStream(res.FileContents)).ReadToEnd();
                var parsed = XDocumentXamlParser.Parse(xaml);

                var initialRoot = (XamlAstObjectNode) parsed.Root;

                var classDirective = initialRoot.Children.OfType<XamlAstXmlDirective>()
                    .FirstOrDefault(d => d.Name == "Class");
                string classname;
                if (classDirective != null && classDirective.Values[0] is XamlAstTextNode tn)
                {
                    classname = tn.Text;
                }
                else
                {
                    classname = res.Name;
                }

                var classType = typeSystem.TargetAssembly.FindType(classname);
                if (classType == null)
                    throw new Exception($"Unable to find type '{classname}'");

                //TODO compiler.Transform?

                var populateName = $"Populate:{res.Name}";
                var buildName = $"Build:{res.Name}";

                var classTypeDefinition = typeSystem.GetTypeReference(classType).Resolve();

                var populateBuilder = typeSystem.CreateTypeBuilder(classTypeDefinition);


                //TODO
                compiler.Compile(parsed, contextClass,
                    compiler.DefinePopulateMethod(populateBuilder, parsed, populateName,
                        classTypeDefinition == null),
                    null, //dont need build
                    null, //might not need this? builder.DefineSubType(compilerConfig.WellKnownTypes.Object, "NamespaceInfo:" + res.Name, true)
                    (closureName, closureBaseType) =>
                        populateBuilder.DefineSubType(closureBaseType, closureName, false),
                    res.Uri, res
                );


                var compiledPopulateMethod = typeSystem.GetTypeReference(populateBuilder).Resolve().Methods
                    .First(m => m.Name == populateName);

                const string TrampolineName = "!XamlIlPopulateTrampoline";
                var trampoline = new MethodDefinition(TrampolineName,
                    MethodAttributes.Static | MethodAttributes.Private, asm.MainModule.TypeSystem.Void);
                trampoline.Parameters.Add(new ParameterDefinition(classTypeDefinition));
                classTypeDefinition.Methods.Add(trampoline);

                //TODO there was a designloader here, is that important? i think not but meh

                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Call, compiledPopulateMethod));
                trampoline.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                var foundXamlLoader = false;
                // Find AvaloniaXamlLoader.Load(this) and replace it with !XamlIlPopulateTrampoline(this)
                foreach (var method in classTypeDefinition.Methods
                    .Where(m => !m.Attributes.HasFlag(MethodAttributes.Static)))
                {
                    var i = method.Body.Instructions;
                    for (var c = 1; c < i.Count; c++)
                    {
                        if (i[c].OpCode == OpCodes.Call)
                        {
                            var op = i[c].Operand as MethodReference;

                            // TODO: Throw an error
                            // This usually happens when same XAML resource was added twice for some weird reason
                            // We currently support it for dual-named default theme resource
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
                                && op.DeclaringType.FullName == "Robust.Shared.Markup.Xaml.RobustXamlLoader"
                            ) //TODO OWN MARKERFUNC IN SHARED OR SMTH
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
                    throw new Exception("No XamlLoader found!");

                return true;
            }
            //TODO CALL build func
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
