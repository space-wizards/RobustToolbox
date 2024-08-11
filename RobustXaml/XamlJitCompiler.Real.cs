#if DEBUG

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using XamlX.IL;
using XamlX.Parsers;

namespace RobustXaml
{
    /// <summary>
    /// A JIT compiler for Xaml.
    ///
    /// Uses System.Reflection.Emit, which can find types at runtime without looking for their assemblies on disk.
    ///
    /// The generated code doesn't respect the sandbox, so this is locked behind DEBUG. (since we're apparently
    /// not given the option of locking it behind TOOLS.)
    /// </summary>
    public sealed class XamlJitCompiler
    {
        private readonly SreTypeSystem _typeSystem;

        private static uint _assemblyId;

        /// <summary>
        /// Construct a XamlJitCompiler.
        ///
        /// No configuration is needed or possible.
        ///
        /// This is a pretty expensive function because it creates an SreTypeSystem, which requires going through
        /// the loaded assembly list.
        /// </summary>
        public XamlJitCompiler()
        {
            _typeSystem = new SreTypeSystem();
        }

        /// <summary>
        /// Generate a name for a new dynamic assembly.
        ///
        /// An effort is made to make the name unique. (even though I am not sure .NET requires this)
        /// </summary>
        /// <returns>the new name</returns>
        private static string GenerateAssemblyName()
        {
            return
                $"{nameof(XamlJitCompiler)}_{Interlocked.Increment(ref _assemblyId)}";
        }

        /// <summary>
        /// Compile the Populate method for `t`, using the given uri/path/contents.
        ///
        /// These values (except for contents) are generated during the AOT compile process.
        ///
        /// It is not enforced that they be the same after JIT -- the JITed code has no knowledge
        /// of the state of the AOT'ed code -- but our code and documentation do assume that.
        /// </summary>
        /// <param name="t">the type whose Populate method should be generated</param>
        /// <param name="uri">the Uri associated with the Control</param>
        /// <param name="filePath">the resource file path for the control</param>
        /// <param name="contents">the contents of the new XAML file</param>
        /// <returns>Success or Failure depending on whether an error was thrown</returns>
        public XamlJitCompilerResult Compile(
            Type t,
            Uri uri,
            string filePath,
            string contents
        )
        {
            try
            {
                var result = CompileOrCrash(t, uri, filePath, contents);
                return new XamlJitCompilerResult.Success(result);
            }
            catch (Exception e)
            {
                return new XamlJitCompilerResult.Error(
                    e,
                    e.Message.StartsWith("Unable to resolve type")
                        ? "Is the type internal? (hot reloading can't handle that right now!)"
                        : null
                );
            }
        }

        private MethodInfo CompileOrCrash(
            Type t,
            Uri uri,
            string filePath,
            string contents
        )
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

            return implementation;
        }

    }
}


/// <summary>
/// An enum containing either Success (with a MethodInfo) or Error.
/// (with an Exception, and an optional hint for how to fix it)
///
/// It is not guaranteed that the Exception ever appeared on the stack.
/// That is an implementation detail of XamlJitCompiler.Compile.
/// </summary>
public abstract record XamlJitCompilerResult
{
    public record Success(MethodInfo MethodInfo): XamlJitCompilerResult;
    public record Error(Exception Raw, string? Hint): XamlJitCompilerResult;
}
#endif
