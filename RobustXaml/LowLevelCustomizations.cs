using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using XamlX.TypeSystem;

namespace RobustXaml
{

    public sealed class LowLevelCustomizations
    {
        public const string TrampolineName = "!XamlIlPopulateTrampoline";
        public const int ExpectedNMetadataArgs = 3;

        private readonly CecilTypeSystem _typeSystem;
        private readonly AssemblyDefinition _asm;

        private readonly TypeDefinition _iocManager;
        private readonly TypeDefinition _xamlProxyHelper;
        private readonly TypeDefinition _systemType;
        private readonly TypeDefinition _stringType;
        private readonly TypeDefinition _xamlMetadataAttributeType;

        private readonly MethodReference _resolveXamlProxyHelperMethod;
        private readonly MethodReference _populateMethod;
        private readonly MethodReference _getTypeFromHandleMethod;
        private readonly MethodReference _xamlMetadataAttributeConstructor;

        public LowLevelCustomizations(CecilTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            _asm = typeSystem.TargetAssemblyDefinition;

            TypeDefinition ResolveType(string name) =>
                typeSystem.GetTypeReference(_typeSystem.FindType(name)).Resolve()
                ?? throw new NullReferenceException($"type must exist: {name}");

            _iocManager = ResolveType("Robust.Shared.IoC.IoCManager");
            _xamlProxyHelper = ResolveType(
                "Robust.Client.UserInterface.XAML.Proxy.XamlProxyHelper"
            );
            _resolveXamlProxyHelperMethod = _asm.MainModule.ImportReference(
                _iocManager.Methods
                    .First(m => m.Name == "Resolve")
                    .MakeGenericMethod(_xamlProxyHelper)
            );

            _populateMethod = _asm.MainModule.ImportReference(
                _xamlProxyHelper.Methods
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

        private MethodDefinition CreateTrampoline(TypeDefinition subject, MethodDefinition aotPopulateMethod)
        {
            // generates on Container:
            //
            // void TrampolineName(Subject subject) {
            //   if (IoCManager.Resolve<XamlProxyHelper>().Populate(typeof(Subject), subject)) {
            //     return;
            //   }
            //   aotPopulateMethod(null, subject)
            // }
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
            Emit(Instruction.Create(OpCodes.Call, _populateMethod));

            var ret = Instruction.Create(OpCodes.Ret);
            Emit(Instruction.Create(OpCodes.Brtrue_S, ret));
            Emit(Instruction.Create(OpCodes.Ldnull));
            Emit(Instruction.Create(OpCodes.Ldarg_0));
            Emit(Instruction.Create(OpCodes.Call, aotPopulateMethod));
            Emit(ret);

            return trampoline;
        }

        public bool TrampolineCallsToXamlLoader(TypeDefinition subject, MethodDefinition aotPopulateMethod)
        {
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

        public void AddXamlMetadata(TypeDefinition subject, Uri uri, string filename, string content)
        {
            var attribute = new CustomAttribute(_xamlMetadataAttributeConstructor);
            var args = new string[ExpectedNMetadataArgs]
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
}
