using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using XamlX.TypeSystem;

namespace Robust.Xaml;

/// <summary>
/// Class that performs find/replace operations on IL in assemblies that contain
/// SS14 content.
/// </summary>
/// <remarks>
/// This code used to live in Robust.Client.Injectors.
///
/// Paul Ritter wrote a lot of code that does low-level Cecil based patching
/// of AoT-compiled XamlX code.
///
/// That's "fine" (it's not actually fine) -- this class just moves that all
/// to one place, and removes the extremely verbose Cecil-based type lookups
/// to a separate shared location.
/// </remarks>
internal sealed class LowLevelCustomizations
{
    public const string TrampolineName = "!XamlIlPopulateTrampoline";
    public const int ExpectedNMetadataArgs = 3;

    private readonly CecilTypeSystem _typeSystem;
    private readonly AssemblyDefinition _asm;

    private readonly TypeDefinition _iocManager;
    private readonly TypeDefinition _iXamlProxyHelper;
    private readonly TypeDefinition _systemType;
    private readonly TypeDefinition _stringType;
    private readonly TypeDefinition _xamlMetadataAttributeType;

    private readonly MethodReference _resolveXamlProxyHelperMethod;
    private readonly MethodReference _populateMethod;
    private readonly MethodReference _getTypeFromHandleMethod;
    private readonly MethodReference _xamlMetadataAttributeConstructor;

    /// <summary>
    /// Create a <see cref="LowLevelCustomizations"/> object.
    /// </summary>
    /// <param name="typeSystem">the <see cref="CecilTypeSystem" /></param>
    /// <exception cref="NullReferenceException">if some needed types were undefined</exception>
    public LowLevelCustomizations(CecilTypeSystem typeSystem)
    {
        // resolve every type that we look for or substitute in when doing surgery
        // what a mess!
        _typeSystem = typeSystem;
        _asm = typeSystem.TargetAssemblyDefinition;

        TypeDefinition ResolveType(string name) =>
            typeSystem.GetTypeReference(_typeSystem.FindType(name)).Resolve()
            ?? throw new NullReferenceException($"type must exist: {name}");

        _iocManager = ResolveType("Robust.Shared.IoC.IoCManager");
        _iXamlProxyHelper = ResolveType(
            "Robust.Client.UserInterface.XAML.Proxy.IXamlProxyHelper"
        );
        _resolveXamlProxyHelperMethod = _asm.MainModule.ImportReference(
            _iocManager.Methods
                .First(m => m.Name == "Resolve")
                .MakeGenericMethod(_iXamlProxyHelper)
        );

        _populateMethod = _asm.MainModule.ImportReference(
            _iXamlProxyHelper.Methods
                .First(m => m.Name == "Populate")
        );

        _systemType = ResolveType("System.Type");

        _getTypeFromHandleMethod = _asm.MainModule.ImportReference(
            _systemType.Resolve()
                .Methods
                .First(m => m.Name == "GetTypeFromHandle")
        );

        _stringType = ResolveType("System.String");

        _xamlMetadataAttributeType = ResolveType(
            "Robust.Client.UserInterface.XAML.Proxy.XamlMetadataAttribute"
        );

        _xamlMetadataAttributeConstructor = _asm.MainModule.ImportReference(
            _xamlMetadataAttributeType
                .GetConstructors()
                .First(
                    c => c.Parameters.Count == ExpectedNMetadataArgs &&
                         c.Parameters.All(p => p.ParameterType.FullName == "System.String")
                )
        );
    }

    /// <summary>
    /// Creates a "trampoline" -- this is a method on the given subject which has the following general logic:
    ///
    /// <code>
    /// void TrampolineName(Subject subject) {
    ///   if (IoCManager.Resolve{XamlProxyHelper}().Populate(typeof(Subject), subject)) {
    ///     return;
    ///   }
    ///   aotPopulateMethod(null, subject)
    /// }
    /// </code>
    ///
    /// </summary>
    /// <param name="subject">the type to create a trampoline on</param>
    /// <param name="aotPopulateMethod">the populate method to call if XamlProxyHelper's Populate method returns false</param>
    /// <returns>the new trampoline method</returns>
    private MethodDefinition CreateTrampoline(TypeDefinition subject, MethodDefinition aotPopulateMethod)
    {
        var trampoline = new MethodDefinition(
            TrampolineName,
            MethodAttributes.Static | MethodAttributes.Private,
            _asm.MainModule.TypeSystem.Void
        );
        trampoline.Parameters.Add(new ParameterDefinition(subject));
        subject.Methods.Add(trampoline);

        void Emit(Instruction i) => trampoline.Body.Instructions.Add(i);

        Emit(Instruction.Create(OpCodes.Call, _resolveXamlProxyHelperMethod));
        Emit(Instruction.Create(OpCodes.Ldtoken, subject));
        Emit(Instruction.Create(OpCodes.Call, _getTypeFromHandleMethod));
        Emit(Instruction.Create(OpCodes.Ldarg_0));
        Emit(Instruction.Create(OpCodes.Callvirt, _populateMethod));

        var ret = Instruction.Create(OpCodes.Ret);
        Emit(Instruction.Create(OpCodes.Brtrue_S, ret));
        Emit(Instruction.Create(OpCodes.Ldnull));
        Emit(Instruction.Create(OpCodes.Ldarg_0));
        Emit(Instruction.Create(OpCodes.Call, aotPopulateMethod));
        Emit(ret);

        return trampoline;
    }

    /// <summary>
    /// Creates a trampoline on <paramref name="subject" />, then replaces
    /// calls to RobustXamlLoader.Load with calls to the generated trampoline.
    /// Returns true if the patching succeeded.
    /// </summary>
    /// <param name="subject">the subject</param>
    /// <param name="aotPopulateMethod">the populate method</param>
    /// <returns>true</returns>
    public bool TrampolineCallsToXamlLoader(TypeDefinition subject, MethodDefinition aotPopulateMethod)
    {
        // PYREX NOTE: This logic is brittle and has a lot of cases
        // I do not understand all of them, but I have faithfully ported them
        // Paul Ritter wrote most of this
        var trampoline = CreateTrampoline(subject, aotPopulateMethod);

        var foundXamlLoader = false;
        // Find RobustXamlLoader.Load(this) and replace it with !XamlIlPopulateTrampoline(this)
        foreach (var method in subject.Methods
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
            var ctors = subject.GetConstructors()
                .Where(c => !c.IsStatic)
                .ToList();
            // We can inject xaml loader into default constructor
            if (ctors.Count == 1 && ctors[0].Body.Instructions.Count(o => o.OpCode != OpCodes.Nop) == 3)
            {
                var i = ctors[0].Body.Instructions;
                var retIdx = i.IndexOf(i.Last(x => x.OpCode == OpCodes.Ret));
                i.Insert(retIdx, Instruction.Create(OpCodes.Call, trampoline));
                i.Insert(retIdx, Instruction.Create(OpCodes.Ldarg_0));
                foundXamlLoader = true;
            }
        }

        return foundXamlLoader;
    }

    private static bool MatchThisCall(Collection<Instruction> instructions, int idx)
    {
        var i = instructions[idx];
        // A "normal" way of passing `this` to a static method:

        // ldarg.0
        // call void [uAvalonia.Markup.Xaml]Avalonia.Markup.Xaml.AvaloniaXamlLoader::Load(object)

        return i.OpCode == OpCodes.Ldarg_0 || (i.OpCode == OpCodes.Ldarg && i.Operand?.Equals(0) == true);
    }

    /// <summary>
    /// Add a XamlMetadataAttribute to a given type, containing all the compiler
    /// parameters for its Populate method.
    /// </summary>
    /// <param name="subject">the subject type</param>
    /// <param name="uri">the URI we generated</param>
    /// <param name="filename">the filename</param>
    /// <param name="content">the new content</param>
    public void AddXamlMetadata(TypeDefinition subject, Uri uri, string filename, string content)
    {
        var attribute = new CustomAttribute(_xamlMetadataAttributeConstructor);
        var args = new string[ExpectedNMetadataArgs]  // reference this so that changing the number is a compile error
        {
            uri.ToString(), filename, content
        };

        foreach (var arg in args)
        {
            attribute.ConstructorArguments.Add(
                new CustomAttributeArgument(_stringType, arg)
            );
        }

        subject.CustomAttributes.Add(attribute);
    }

}
