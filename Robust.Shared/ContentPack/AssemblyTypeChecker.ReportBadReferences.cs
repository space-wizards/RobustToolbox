#if TOOLS

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace Robust.Shared.ContentPack;

internal sealed partial class AssemblyTypeChecker
{
    // This part of the code tries to find the originator of bad sandbox references.

    private void ReportBadReferences(PEReader peReader, MetadataReader reader, IEnumerable<EntityHandle> reference)
    {
        _sawmill.Info("Started search for originator of bad references...");

        var refs = reference.ToHashSet();
        ExpandReferences(reader, refs);

        foreach (var methodDefHandle in reader.MethodDefinitions)
        {
            var methodDef = reader.GetMethodDefinition(methodDefHandle);
            if (methodDef.RelativeVirtualAddress == 0)
                continue;

            var methodName = reader.GetString(methodDef.Name);

            var body = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
            var bytes = body.GetILBytes()!;

            var ilReader = new ILReader(bytes);
            var prefPosition = 0;
            while (ilReader.MoveNext(out var instruction))
            {
                if (instruction.TryGetEntityHandle(out var handle))
                {
                    if (refs.Overlaps(ExpandHandle(reader, handle)))
                    {
                        var type = GetTypeFromDefinition(reader, methodDef.GetDeclaringType());
                        _sawmill.Error(
                            $"Found reference to {DisplayHandle(reader, handle)} in method {type}.{methodName} at IL 0x{prefPosition:X4}");
                    }
                }

                prefPosition = ilReader.Position;
            }
        }
    }

    private static string DisplayHandle(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodSpecification:
                var methodSpec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                var methodProvider = new TypeProvider();
                var spec = methodSpec.DecodeSignature(methodProvider, 0);
                return $"{DisplayHandle(reader, methodSpec.Method)}<{string.Join(", ", spec.Select(t => t.ToString()))}>";

            case HandleKind.MemberReference:
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)handle);
                var name = reader.GetString(memberRef.Name);
                var parent = DisplayHandle(reader, memberRef.Parent);
                return $"{parent}.{name}";

            case HandleKind.TypeReference:
                return $"{ParseTypeReference(reader, (TypeReferenceHandle)handle)}";

            case HandleKind.TypeSpecification:
                var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
                var provider = new TypeProvider();
                var type = typeSpec.DecodeSignature(provider, 0);
                return $"{type}";

            default:
                return $"({handle.Kind} handle)";
        }
    }

    private static void ExpandReferences(MetadataReader reader, HashSet<EntityHandle> handles)
    {
        var toAdd = new List<EntityHandle>();

        foreach (var memberRefHandle in reader.MemberReferences)
        {
            var memberRef = reader.GetMemberReference(memberRefHandle);
            if (handles.Contains(memberRef.Parent))
            {
                toAdd.Add(memberRefHandle);
            }
        }

        handles.UnionWith(toAdd);
    }

    private static IEnumerable<EntityHandle> ExpandHandle(MetadataReader reader, EntityHandle handle)
    {
        // Annoying, S.R.M gives no way to iterate over the MethodSpec table.
        // This means the only way to correlate MethodSpec references is to do it for each handle.

        yield return handle;

        if (handle.Kind == HandleKind.MethodSpecification)
            yield return reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
    }

    private readonly struct ILInstruction
    {
        public readonly ILOpCode OpCode;
        public readonly long Argument;
        public readonly int[]? SwitchTargets;

        public ILInstruction(ILOpCode opCode)
        {
            OpCode = opCode;
        }

        public ILInstruction(ILOpCode opCode, long argument)
        {
            OpCode = opCode;
            Argument = argument;
        }

        public ILInstruction(ILOpCode opCode, long argument, int[] switchTargets)
        {
            OpCode = opCode;
            Argument = argument;
            SwitchTargets = switchTargets;
        }

        public bool TryGetEntityHandle(out EntityHandle handle)
        {
            switch (OpCode)
            {
                case ILOpCode.Call:
                case ILOpCode.Callvirt:
                case ILOpCode.Newobj:
                case ILOpCode.Jmp:
                case ILOpCode.Box:
                case ILOpCode.Castclass:
                case ILOpCode.Cpobj:
                case ILOpCode.Initobj:
                case ILOpCode.Isinst:
                case ILOpCode.Ldelem:
                case ILOpCode.Ldelema:
                case ILOpCode.Ldfld:
                case ILOpCode.Ldflda:
                case ILOpCode.Ldobj:
                case ILOpCode.Ldstr:
                case ILOpCode.Ldtoken:
                case ILOpCode.Ldvirtftn:
                case ILOpCode.Mkrefany:
                case ILOpCode.Newarr:
                case ILOpCode.Refanyval:
                case ILOpCode.Sizeof:
                case ILOpCode.Stelem:
                case ILOpCode.Stfld:
                case ILOpCode.Stobj:
                case ILOpCode.Stsfld:
                case ILOpCode.Throw:
                case ILOpCode.Unbox_any:
                    handle = Unsafe.BitCast<int, EntityHandle>((int)Argument);
                    return true;

                default:
                    handle = default;
                    return false;
            }
        }
    }

    private sealed class ILReader(byte[] body)
    {
        public int Position;

        public bool MoveNext(out ILInstruction instruction)
        {
            if (Position >= body.Length)
            {
                instruction = default;
                return false;
            }

            var firstByte = body[Position++];
            var opCode = (ILOpCode)firstByte;
            if (firstByte == 0xFE)
                opCode = 0xFE00 + (ILOpCode)body[Position++];

            switch (opCode)
            {
                // no args.
                case ILOpCode.Readonly:
                case ILOpCode.Tail:
                case ILOpCode.Volatile:
                case ILOpCode.Add:
                case ILOpCode.Add_ovf:
                case ILOpCode.Add_ovf_un:
                case ILOpCode.And:
                case ILOpCode.Arglist:
                case ILOpCode.Break:
                case ILOpCode.Ceq:
                case ILOpCode.Cgt:
                case ILOpCode.Cgt_un:
                case ILOpCode.Ckfinite:
                case ILOpCode.Clt:
                case ILOpCode.Clt_un:
                case ILOpCode.Conv_i1:
                case ILOpCode.Conv_i2:
                case ILOpCode.Conv_i4:
                case ILOpCode.Conv_i8:
                case ILOpCode.Conv_r4:
                case ILOpCode.Conv_r8:
                case ILOpCode.Conv_u1:
                case ILOpCode.Conv_u2:
                case ILOpCode.Conv_u4:
                case ILOpCode.Conv_u8:
                case ILOpCode.Conv_i:
                case ILOpCode.Conv_u:
                case ILOpCode.Conv_r_un:
                case ILOpCode.Conv_ovf_i1:
                case ILOpCode.Conv_ovf_i2:
                case ILOpCode.Conv_ovf_i4:
                case ILOpCode.Conv_ovf_i8:
                case ILOpCode.Conv_ovf_u4:
                case ILOpCode.Conv_ovf_u8:
                case ILOpCode.Conv_ovf_i:
                case ILOpCode.Conv_ovf_u:
                case ILOpCode.Conv_ovf_i1_un:
                case ILOpCode.Conv_ovf_i2_un:
                case ILOpCode.Conv_ovf_i4_un:
                case ILOpCode.Conv_ovf_i8_un:
                case ILOpCode.Conv_ovf_u4_un:
                case ILOpCode.Conv_ovf_u8_un:
                case ILOpCode.Conv_ovf_i_un:
                case ILOpCode.Conv_ovf_u_un:
                case ILOpCode.Cpblk:
                case ILOpCode.Div:
                case ILOpCode.Div_un:
                case ILOpCode.Dup:
                case ILOpCode.Endfilter:
                case ILOpCode.Endfinally:
                case ILOpCode.Initblk:
                case ILOpCode.Ldarg_0:
                case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2:
                case ILOpCode.Ldarg_3:
                case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1:
                case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3:
                case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5:
                case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7:
                case ILOpCode.Ldc_i4_8:
                case ILOpCode.Ldc_i4_m1:
                case ILOpCode.Ldind_i1:
                case ILOpCode.Ldind_u1:
                case ILOpCode.Ldind_i2:
                case ILOpCode.Ldind_u2:
                case ILOpCode.Ldind_i4:
                case ILOpCode.Ldind_u4:
                case ILOpCode.Ldind_i8:
                case ILOpCode.Ldind_i:
                case ILOpCode.Ldind_r4:
                case ILOpCode.Ldind_r8:
                case ILOpCode.Ldind_ref:
                case ILOpCode.Ldloc_0:
                case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2:
                case ILOpCode.Ldloc_3:
                case ILOpCode.Ldnull:
                case ILOpCode.Localloc:
                case ILOpCode.Mul:
                case ILOpCode.Mul_ovf:
                case ILOpCode.Mul_ovf_un:
                case ILOpCode.Neg:
                case ILOpCode.Nop:
                case ILOpCode.Not:
                case ILOpCode.Or:
                case ILOpCode.Pop:
                case ILOpCode.Rem:
                case ILOpCode.Rem_un:
                case ILOpCode.Ret:
                case ILOpCode.Shl:
                case ILOpCode.Shr:
                case ILOpCode.Shr_un:
                case ILOpCode.Stind_i1:
                case ILOpCode.Stind_i2:
                case ILOpCode.Stind_i4:
                case ILOpCode.Stind_i8:
                case ILOpCode.Stind_r4:
                case ILOpCode.Stind_r8:
                case ILOpCode.Stind_i:
                case ILOpCode.Stind_ref:
                case ILOpCode.Stloc_0:
                case ILOpCode.Stloc_1:
                case ILOpCode.Stloc_2:
                case ILOpCode.Stloc_3:
                case ILOpCode.Sub:
                case ILOpCode.Sub_ovf:
                case ILOpCode.Sub_ovf_un:
                case ILOpCode.Xor:
                case ILOpCode.Ldelem_i1:
                case ILOpCode.Ldelem_u1:
                case ILOpCode.Ldelem_i2:
                case ILOpCode.Ldelem_u2:
                case ILOpCode.Ldelem_i4:
                case ILOpCode.Ldelem_u4:
                case ILOpCode.Ldelem_i8:
                case ILOpCode.Ldelem_i:
                case ILOpCode.Ldelem_r4:
                case ILOpCode.Ldelem_r8:
                case ILOpCode.Ldelem_ref:
                case ILOpCode.Ldlen:
                case ILOpCode.Refanytype:
                case ILOpCode.Rethrow:
                case ILOpCode.Stelem_i1:
                case ILOpCode.Stelem_i2:
                case ILOpCode.Stelem_i4:
                case ILOpCode.Stelem_i8:
                case ILOpCode.Stelem_i:
                case ILOpCode.Stelem_r4:
                case ILOpCode.Stelem_r8:
                case ILOpCode.Stelem_ref:
                case ILOpCode.Throw:
                    instruction = new ILInstruction(opCode);
                    break;

                // 1-byte arg.
                case ILOpCode.Unaligned:
                case ILOpCode.Beq_s:
                case ILOpCode.Bge_s:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt_s:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble_s:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt_s:
                case ILOpCode.Blt_un_s:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Br_s:
                case ILOpCode.Brfalse_s:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Ldarg_s:
                case ILOpCode.Ldarga_s:
                case ILOpCode.Ldc_i4_s:
                case ILOpCode.Ldloc_s:
                case ILOpCode.Ldloca_s:
                case ILOpCode.Leave_s:
                case ILOpCode.Starg_s:
                case ILOpCode.Stloc_s:
                    instruction = new ILInstruction(opCode, body[Position]);
                    Position += 1;
                    break;

                // 2-byte value
                case ILOpCode.Ldarg:
                case ILOpCode.Ldarga:
                case ILOpCode.Ldloc:
                case ILOpCode.Ldloca:
                case ILOpCode.Starg:
                case ILOpCode.Stloc:
                    var shortValue = BinaryPrimitives.ReadInt16LittleEndian(body.AsSpan(Position, 2));
                    Position += 2;
                    instruction = new ILInstruction(opCode, shortValue);
                    break;

                // 4-byte value
                case ILOpCode.Constrained:
                case ILOpCode.Beq:
                case ILOpCode.Bge:
                case ILOpCode.Bge_un:
                case ILOpCode.Bgt:
                case ILOpCode.Bgt_un:
                case ILOpCode.Ble:
                case ILOpCode.Ble_un:
                case ILOpCode.Blt:
                case ILOpCode.Blt_un:
                case ILOpCode.Bne_un:
                case ILOpCode.Br:
                case ILOpCode.Brfalse:
                case ILOpCode.Brtrue:
                case ILOpCode.Call:
                case ILOpCode.Calli:
                case ILOpCode.Jmp:
                case ILOpCode.Ldc_i4:
                case ILOpCode.Ldc_r4:
                case ILOpCode.Ldftn:
                case ILOpCode.Leave:
                case ILOpCode.Box:
                case ILOpCode.Callvirt:
                case ILOpCode.Castclass:
                case ILOpCode.Cpobj:
                case ILOpCode.Initobj:
                case ILOpCode.Isinst:
                case ILOpCode.Ldelem:
                case ILOpCode.Ldelema:
                case ILOpCode.Ldfld:
                case ILOpCode.Ldflda:
                case ILOpCode.Ldobj:
                case ILOpCode.Ldsfld:
                case ILOpCode.Ldsflda:
                case ILOpCode.Ldstr:
                case ILOpCode.Ldtoken:
                case ILOpCode.Ldvirtftn:
                case ILOpCode.Mkrefany:
                case ILOpCode.Newarr:
                case ILOpCode.Newobj:
                case ILOpCode.Refanyval:
                case ILOpCode.Sizeof:
                case ILOpCode.Stelem:
                case ILOpCode.Stfld:
                case ILOpCode.Stobj:
                case ILOpCode.Stsfld:
                case ILOpCode.Unbox:
                case ILOpCode.Unbox_any:
                    var intValue = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(Position, 4));
                    Position += 4;
                    instruction = new ILInstruction(opCode, intValue);
                    break;

                // 8-byte value
                case ILOpCode.Ldc_i8:
                case ILOpCode.Ldc_r8:
                    var longValue = BinaryPrimitives.ReadInt64LittleEndian(body.AsSpan(Position, 8));
                    Position += 8;
                    instruction = new ILInstruction(opCode, longValue);
                    break;

                // Switch
                case ILOpCode.Switch:
                    var switchLength = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(Position, 4));
                    Position += 4;
                    var switchArgs = new int[switchLength];
                    for (var i = 0; i < switchLength; i++)
                    {
                        switchArgs[i] = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(Position, 4));
                        Position += 4;
                    }

                    instruction = new ILInstruction(opCode, switchLength, switchArgs);
                    break;

                default:
                    throw new InvalidDataException($"Unknown opcode: {opCode}");
            }

            return true;
        }
    }
}

#endif
