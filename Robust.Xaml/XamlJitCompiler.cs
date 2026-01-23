using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.TypeSystem;

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

    // Taken from https://github.com/AvaloniaUI/Avalonia/blob/01a8042094d741a8ddfcd441b4eabcb04092e988/src/Markup/Avalonia.Markup.Xaml.Loader/AvaloniaXamlIlRuntimeCompiler.cs#L185
    static Type EmitIgnoresAccessCheckAttributeDefinition(ModuleBuilder builder)
    {
        var tb = builder.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
            TypeAttributes.Class | TypeAttributes.Public, typeof(Attribute));
        var field = tb.DefineField("_name", typeof(string), FieldAttributes.Private);
        var propGet = tb.DefineMethod("get_AssemblyName", MethodAttributes.Public, typeof(string),
            Array.Empty<Type>());
        var propGetIl = propGet.GetILGenerator();
        propGetIl.Emit(OpCodes.Ldarg_0);
        propGetIl.Emit(OpCodes.Ldfld, field);
        propGetIl.Emit(OpCodes.Ret);
        var prop = tb.DefineProperty("AssemblyName", PropertyAttributes.None, typeof(string), Array.Empty<Type>());
        prop.SetGetMethod(propGet);


        var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
            new[] { typeof(string) });
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Ldarg_1);
        ctorIl.Emit(OpCodes.Stfld, field);
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(Attribute)
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == 0));

        ctorIl.Emit(OpCodes.Ret);

        tb.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(AttributeUsageAttribute).GetConstructor(new[] { typeof(AttributeTargets) })!,
            new object[] { AttributeTargets.Assembly },
            new[] { typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))! },
            new object[] { true }));

        return tb.CreateTypeInfo()!;
    }

    // Taken from https://github.com/AvaloniaUI/Avalonia/blob/01a8042094d741a8ddfcd441b4eabcb04092e988/src/Markup/Avalonia.Markup.Xaml.Loader/AvaloniaXamlIlRuntimeCompiler.cs#L185
    static HashSet<Assembly> FindAssembliesGrantingInternalAccess(Assembly assembly)
    {
        var result = new HashSet<Assembly>();
        if (assembly == null)
            return result;

        var assemblyName = assembly.GetName();
        var publicKey = assemblyName.GetPublicKey();

        // Search through all loaded assemblies to find those that grant InternalsVisibleTo to our assembly
        foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var ivtAttributes = loadedAssembly.GetCustomAttributes(
                    typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), false);

                foreach (System.Runtime.CompilerServices.InternalsVisibleToAttribute ivt in ivtAttributes)
                {
                    var ivtName = ivt.AssemblyName;
                    if (string.IsNullOrWhiteSpace(ivtName))
                        continue;

                    // Parse the InternalsVisibleTo assembly name
                    var ivtAssemblyName = new AssemblyName(ivtName);

                    // Check if it matches our assembly name
                    if (string.Equals(ivtAssemblyName.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // If public key is specified in IVT, verify it matches
                        var ivtPublicKey = ivtAssemblyName.GetPublicKey();
                        if (ivtPublicKey != null && ivtPublicKey.Length > 0)
                        {
                            if (publicKey != null && publicKey.SequenceEqual(ivtPublicKey))
                            {
                                result.Add(loadedAssembly);
                            }
                        }
                        else
                        {
                            // No public key specified in IVT, just match by name
                            result.Add(loadedAssembly);
                        }
                    }
                }
            }
            catch
            {
                // Ignore assemblies that throw exceptions when accessing attributes
            }
        }

        return result;
    }

    // Taken from https://github.com/AvaloniaUI/Avalonia/blob/01a8042094d741a8ddfcd441b4eabcb04092e988/src/Markup/Avalonia.Markup.Xaml.Loader/AvaloniaXamlIlRuntimeCompiler.cs#L185
    static void EmitIgnoresAccessCheckToAttribute(AssemblyName assemblyName, Type ignoresAccessChecksFromAttribute, AssemblyBuilder builder)
    {
        var name = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(name))
            return;
        var key = assemblyName.GetPublicKey();
        if (key != null && key.Length != 0)
            name += ", PublicKey=" + BitConverter.ToString(key).Replace("-", "").ToUpperInvariant();
        builder.SetCustomAttribute(new CustomAttributeBuilder(
            ignoresAccessChecksFromAttribute!.GetConstructors()[0],
            new object[] { name }));
    }

    private MethodInfo CompileOrCrash(
        Type t,
        Uri uri,
        string filePath,
        string contents
    )
    {

        var xaml = new XamlCustomizations(_typeSystem, _typeSystem.FindAssembly(t.Assembly.FullName)!);

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
        var declaration = EmitIgnoresAccessCheckAttributeDefinition(moduleBuilder);

        EmitIgnoresAccessCheckToAttribute(t.Assembly.GetName(), declaration, assemblyBuilder);
        foreach (var assembly in FindAssembliesGrantingInternalAccess(t.Assembly))
        {
            EmitIgnoresAccessCheckToAttribute(assembly.GetName(), declaration, assemblyBuilder);
        }

        var contextClassRawBuilder = moduleBuilder.DefineType("ContextClass");
        var populateClassRawBuilder = moduleBuilder.DefineType("PopulateClass");
        var buildClassRawBuilder = moduleBuilder.DefineType("BuildClass");

        var contextClassBuilder = _typeSystem.CreateTypeBuilder(contextClassRawBuilder);
        var populateClassBuilder = _typeSystem.CreateTypeBuilder(populateClassRawBuilder);
        var buildClassBuilder = _typeSystem.CreateTypeBuilder(buildClassRawBuilder);

        var contextClass = XamlILContextDefinition.GenerateContextClass(
            contextClassBuilder,
            _typeSystem,
            xaml.TypeMappings,
            xaml.EmitMappings
        );
        var populateName = "!Populate";
        var buildName = "!Build";

        xaml.ILCompiler.Transform(document);
        xaml.ILCompiler.Compile(
            doc: document,
            contextType: contextClass,
            populateMethod: xaml.ILCompiler.DefinePopulateMethod(
                populateClassBuilder,
                document,
                populateName,
                XamlVisibility.Public
            ),
            populateDeclaringType: populateClassBuilder,
            buildMethod: xaml.ILCompiler.DefineBuildMethod(
                buildClassBuilder,
                document,
                buildName,
                XamlVisibility.Public
            ),
            buildDeclaringType: buildClassBuilder,
            namespaceInfoBuilder: null,
            baseUri: uri.ToString(),
            fileSource: xaml.CreateFileSource(filePath, Encoding.UTF8.GetBytes(contents))
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
