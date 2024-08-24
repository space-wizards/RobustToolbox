using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using XamlX.IL;
using XamlX.Parsers;

namespace Robust.Xaml;

/// <summary>
/// A JIT compiler for Xaml.
/// </summary>
/// <remarks>
/// Uses <see cref="System.Reflection.Emit"/>, which can find types
/// at runtime without looking for their assemblies on disk.
/// </remarks>
internal sealed class XamlJitCompiler
{
    private readonly SreTypeSystem _typeSystem;

    private static int _assemblyId;

    /// <summary>
    /// Construct a XamlJitCompiler.
    /// </summary>
    /// <remarks>
    /// No configuration is needed or possible.
    ///
    /// This is a pretty expensive function because it creates an
    /// <see cref="SreTypeSystem"/>, which requires going through the loaded
    /// assembly list.
    /// </remarks>
    public XamlJitCompiler()
    {
        _typeSystem = new SreTypeSystem();
    }

    /// <summary>
    /// Generate a name for a new dynamic assembly.
    /// </summary>
    /// <returns>the new name</returns>
    private static string GenerateAssemblyName()
    {
        // make the name unique (even though C# possibly doesn't care)
        return
            $"{nameof(XamlJitCompiler)}_{Interlocked.Increment(ref _assemblyId)}";
    }

    /// <summary>
    /// Compile the Populate method for <paramref name="t" />, using the given
    /// uri/path/contents.
    /// </summary>
    /// <remarks>
    /// These values (except for contents) are generated during the AOT compile
    /// process.
    ///
    /// It is not enforced that they be the same after JIT -- the JITed code has
    /// no knowledge of the state of the AOT'ed code -- but our code and
    /// documentation do assume that.
    /// </remarks>
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

        var populateClass = populateClassRawBuilder.CreateTypeInfo()!;
        var implementation = populateClass.GetMethod(populateName);

        if (implementation == null)
        {
            throw new NullReferenceException("populate method should have existed");
        }

        return implementation;
    }

}

/// <summary>
/// An enum containing either <see cref="Success" /> (with a <see cref="MethodInfo" />)
/// or <see cref="Error" />. (with an <see cref="Exception" />, and an optional hint
/// for how to fix it)
/// </summary>
/// <remarks>
/// It is not guaranteed that the <see cref="Exception" /> ever appeared on the stack.
/// That is an implementation detail of <see cref="XamlJitCompiler.Compile"/>.
/// </remarks>
public abstract class XamlJitCompilerResult
{
    public class Success(MethodInfo methodInfo) : XamlJitCompilerResult
    {
        public MethodInfo MethodInfo { get; } = methodInfo;
    }

    public class Error(Exception raw, string? hint) : XamlJitCompilerResult
    {
        public Exception Raw { get; } = raw;
        public string? Hint { get; } = hint;
    }
}
