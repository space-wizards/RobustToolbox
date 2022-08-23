using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Robust.Shared.Utility
{
    public static class ILGeneratorExt
    {
        public static RobustILGenerator GetRobustGen(this DynamicMethod dynamicMethod) =>
            new(dynamicMethod.GetILGenerator());
        public static RobustILGenerator GetRobustGen(this ILGenerator generator) => new (generator);

    }

    public sealed class RobustILGenerator
    {
        private ILGenerator _generator;
        private List<(object, object?)> _log = new();
        private List<(Type, int)> _locals = new();

        public RobustILGenerator(ILGenerator generator)
        {
            _generator = generator;
        }

        public string[] GetStringLog()
        {
            var full = _locals.Select(e => $"LOCAL: {e.Item1} at {e.Item2}").ToList();
            full.Add("===== CODE =====");
            full.AddRange(_log.Select(e => $"{e.Item1} - {e.Item2}"));
            return full.ToArray();
        }

        private void Log(object opcode, object? arg = null)
        {
            _log.Add((opcode, arg));
        }

        public LocalBuilder DeclareLocal(Type localType)
        {
            return DeclareLocal(localType, false);
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            var loc = _generator.DeclareLocal(localType, pinned);
            _locals.Add((localType, loc.LocalIndex));
            return loc;
        }

        public void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
        {
            //this just calls emit
            _generator.ThrowException(excType);
        }

        public Label DefineLabel()
        {
            var label = _generator.DefineLabel();
            //Log("DefineLabel", label);
            return label;
        }

        public void MarkLabel(Label loc)
        {
            _generator.MarkLabel(loc);
            Log("MarkLabel", loc.GetHashCode());
        }

        public void Emit(OpCode opcode)
        {
            _generator.Emit(opcode);
            Log(opcode);
        }

        public void Emit(OpCode opcode, byte arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, short arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, int arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, MethodInfo meth)
        {
            _generator.Emit(opcode, meth);
            Log(opcode, meth);
        }

        // public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention,
        //     Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes)
        // {
        // }
        //
        // public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
        // {
        // }
        //

        public void EmitCall(OpCode opcode, MethodInfo methodInfo, params Type[]? optionalParameterTypes)
        {
            _generator.EmitCall(opcode, methodInfo, optionalParameterTypes);
            Log(opcode, methodInfo);
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
            _generator.Emit(opcode, signature);
            Log(opcode, signature);
        }

        public void Emit(OpCode opcode, ConstructorInfo con)
        {
            _generator.Emit(opcode, con);
            Log(opcode, con);
        }

        public void Emit(OpCode opcode, Type cls)
        {
            _generator.Emit(opcode, cls);
            Log(opcode, cls);
        }

        public void Emit(OpCode opcode, long arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, float arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, double arg)
        {
            _generator.Emit(opcode, arg);
            Log(opcode, arg);
        }

        public void Emit(OpCode opcode, Label label)
        {
            _generator.Emit(opcode, label);
            Log(opcode, label.GetHashCode());
        }

        public void Emit(OpCode opcode, Label[] labels)
        {
            _generator.Emit(opcode, labels);
            Log(opcode, labels);
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
            _generator.Emit(opcode, field);
            Log(opcode, field);
        }

        public void Emit(OpCode opcode, string str)
        {
            _generator.Emit(opcode, str);
            Log(opcode, str);
        }

        public void Emit(OpCode opcode, LocalBuilder local)
        {
            _generator.Emit(opcode, local);
            Log(opcode, local);
        }
    }
}
