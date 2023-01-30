using System;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables
{
    /// <summary>
    ///     Attribute to make a property or field accessible to VV.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Virtual]
    public class ViewVariablesAttribute : Attribute
    {
        /// <summary>
        ///     These access permissions control whether a field or property is readonly or writable. Methods are always
        ///     invokable.
        /// </summary>
        public readonly VVAccess Access = VVAccess.ReadOnly;

        public ViewVariablesAttribute()
        {

        }

        public ViewVariablesAttribute(VVAccess access)
        {
            Access = access;
        }
    }

    /// <summary>
    ///     Attribute to make a method invokable via VV.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class VVInvokableAttribute : ViewVariablesAttribute
    {
        public VVInvokableAttribute() : base(VVAccess.Execute)
        {
        }
    }

    [Flags, Serializable, NetSerializable]
    public enum VVAccess : byte
    {
        ReadOnly =  1 << 0,
        Write =     1 << 1,
        Execute =   1 << 2,

        ReadWrite = ReadOnly | Write,
        All = ReadWrite | Execute
    }
}
