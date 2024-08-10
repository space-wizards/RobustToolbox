using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using XamlX.IL;
using XamlX.Parsers;

namespace RobustXaml
{

    public sealed class XamlJitCompiler
    {
        private readonly SreTypeSystem _typeSystem;

        private static uint _assemblyId;

        public XamlJitCompiler()
        {
            // PYREX NOTE: This is extremely expensive to create
            // So try to do it only when the assembly list changes!
            _typeSystem = new SreTypeSystem();
        }

        private static string GenerateAssemblyName()
        {
            return
                $"{nameof(XamlJitCompiler)}_{Interlocked.Increment(ref _assemblyId)}";
        }

        public XamlJitCompilerResult CompilePopulate(
            Type t,
            Uri uri,
            string filePath,
            string contents,
            Action<Assembly> runLastMinuteAssertions)
        {
            try
            {
                var result = CompilePopulateOrCrash(t, uri, filePath, contents, runLastMinuteAssertions);
                return new XamlJitCompilerResult.Success(result);
            }
            catch (XamlX.XamlParseException xpe)
            {
                return new XamlJitCompilerResult.Error(
                    xpe,
                    xpe.Message.StartsWith("Unable to resolve type")
                        ? "Is the type internal? (hot reloading can't handle that right now!)"
                        : null
                );
            }
        }

        private MethodInfo CompilePopulateOrCrash(
            Type t,
            Uri uri,
            string filePath,
            string contents,
            Action<Assembly> runLastMinuteAssertions)
        {

            var xaml = new XamlCustomizations(_typeSystem, _typeSystem.FindAssembly(t.Assembly.FullName));

            // attempt to parse the code
            var document = XDocumentXamlParser.Parse(contents);

            // generate a toy assembly to contain everything we make
            var assemblyNameString = GenerateAssemblyName();
            var assemblyName = new AssemblyName(assemblyNameString);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.RunAndCollect
            );
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyNameString);

            var contextClassRawBuilder = moduleBuilder.DefineType("ContextClass");
            var populateClassRawBuilder = moduleBuilder.DefineType("PopulateClass");

            var contextClassBuilder = _typeSystem.CreateTypeBuilder(contextClassRawBuilder);
            var populateClassBuilder = _typeSystem.CreateTypeBuilder(populateClassRawBuilder);

            var contextClass = XamlILContextDefinition.GenerateContextClass(
                contextClassBuilder,
                _typeSystem,
                xaml.TypeMappings,
                xaml.EmitMappings
            );
            var populateName = "!Populate";

            xaml.ILCompiler.Transform(document);
            xaml.ILCompiler.Compile(
                document,
                contextClass,
                xaml.ILCompiler.DefinePopulateMethod(
                    populateClassBuilder,
                    document,
                    populateName,
                    true
                ),
                null,
                null,
                (closureName, closureBaseType) =>
                    contextClassBuilder.DefineSubType(closureBaseType, closureName, false),
                uri.ToString(),
                xaml.CreateFileSource(filePath, Encoding.UTF8.GetBytes(contents))
            );

            contextClassBuilder.CreateType();

            var populateClass = populateClassRawBuilder.CreateType()!;
            var implementation = populateClass.GetMethod(populateName);

            if (implementation == null)
            {
                throw new NullReferenceException("populate method should have existed");
            }

            // Here's where you can check your sandboxing.
            //
            // Sandboxing note: I'm not clear on whether creating an Assembly that references a type will load the
            // static constructor of that type. If so, the sandboxing here is imperfect.
            //
            // So, to be safe, this should only be used in debug builds. (at least until SreTypeSystem is replaced
            // with something that enforces an allowlist, emits Cecil, or both)
            runLastMinuteAssertions(populateClass.Assembly);

            return implementation;
        }

    }
}


public abstract record XamlJitCompilerResult
{
    public record Success(MethodInfo MethodInfo): XamlJitCompilerResult;
    public record Error(Exception Raw, string? Hint): XamlJitCompilerResult;
}
