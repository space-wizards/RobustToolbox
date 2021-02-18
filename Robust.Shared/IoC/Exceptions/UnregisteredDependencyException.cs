using System;
using System.Runtime.Serialization;

namespace Robust.Shared.IoC.Exceptions
{
    /// <summary>
    /// Like <see cref="UnregisteredTypeException"/>,
    /// except that this is thrown when using field injection via <see cref="DependencyAttribute"/> and includes extra metadata.
    /// </summary>
    [Serializable]
    public class UnregisteredDependencyException : Exception
    {
        /// <summary>
        /// The type name of the type requesting the unregistered dependency.
        /// </summary>
        public readonly string? OwnerType;

        /// <summary>
        /// The type name of the type that was requested and unregistered.
        /// </summary>
        public readonly string? TargetType;

        /// <summary>
        /// The name of the field that was marked as dependency.
        /// </summary>
        public readonly string? FieldName;

        public UnregisteredDependencyException(Type owner, Type target, string fieldName)
            : base(string.Format("{0} requested unregistered type with its field {1}: {2}",
                                 owner, target, fieldName))
        {
            OwnerType = owner.AssemblyQualifiedName;
            TargetType = target.AssemblyQualifiedName;
            FieldName = fieldName;
        }

        protected UnregisteredDependencyException(
          SerializationInfo info,
          StreamingContext context) : base(info, context)
        {
            OwnerType = info.GetString("OwnerType");
            TargetType = info.GetString("TargetType");
            FieldName = info.GetString("FieldName");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("OwnerType", OwnerType);
            info.AddValue("TargetType", TargetType);
            info.AddValue("FieldName", FieldName);
        }
    }
}
