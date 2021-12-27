using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Utility class for producing debug menu representations.
    /// </summary>
    public static class PrettyPrint
    {

        /// <summary>
        ///     Get the user-facing string representation of a value.
        ///
        ///     This is intended for menus where users are required to look at
        ///     some kind of raw engine representation. It is not a substitute
        ///     for a proper UI.
        /// </summary>
        /// <param name="value">The object to represent.</param>
        /// <returns>A readable representation of the object.</returns>
        public static string? PrintUserFacing(object? value)
        {
           return PrintUserFacingWithType(value, out _);
        }

        /// <summary>
        ///     Get the user-facing string representation of a value, along with
        ///     the representation of its type.
        ///
        ///     See <see cref='PrintUserFacing(object)'/> for usage details. This
        ///     also returns a user-facing representation of the object's type in
        ///     <paramref name="typeRep"/> if it is different to that of the object.
        ///     If the object's <c>ToString()</c> implementation is the default
        ///     one, then <paramref name="typeRep"/> will be <c>""</c>.
        /// </summary>
        /// <param name="value">The object to represent.</param>
        /// <param name="typeRef">
        ///   The representation of the object's type, if distinct from the
        ///   returned value. Otherwise, <c>""</c>.
        /// </param>
        /// <returns>A readable representation of the object.</returns>
        public static string PrintUserFacingWithType(object? value, out string typeRep)
        {
            if (value == null) {
                typeRep = string.Empty;
                return "null";
            }

            string? stringRep;
            // Make best effort to guess whether or not this needs an abbreviated
            // type representation - if the type doesn't overwrite the default
            // `Object` `ToString`, then it will just print a type - so we instead
            // print the abbreviated version. Otherwise let the type print whatever
            // it wants
            if (value.GetType().GetMethod("ToString", new Type[0], new ParameterModifier[0])!.DeclaringType == typeof(Object)) {
                stringRep = TypeAbbreviation.Abbreviate(value.GetType());
                typeRep = string.Empty;
            } else if (value is EntityUid uid)
            {
                stringRep = IoCManager.Resolve<IEntityManager>().ToPrettyString(uid);
                typeRep = TypeAbbreviation.Abbreviate(value.GetType());
            }else {
                stringRep = value.ToString();
                typeRep = TypeAbbreviation.Abbreviate(value.GetType());
            }

            return stringRep!;
        }

        public static string PrintTypeSignature(this Type type)
        {
            if (TypeShortHand.TryGetValue(type, out var value))
                return value;

            var nullable = false;
            if (type.IsNullable(out var underlying))
            {
                type = underlying;
                nullable = true;
            }

            if (type.IsGenericType)
            {
                return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(PrintTypeSignature))}>{(nullable ? "?" : string.Empty)}";
            }

            return type.Name;
        }

        public static string PrintParameterSignature(this ParameterInfo parameter)
        {
            var builder = new StringBuilder();

            if (parameter.IsIn)
            {
                builder.Append($"in ");
            }

            if (parameter.IsOut)
            {
                builder.Append($"out ");
            }
            else if (parameter.ParameterType.IsByRef)
            {
                builder.Append($"ref ");
            }

            builder.Append(parameter.ParameterType.PrintTypeSignature());

            if (parameter.Name is { } name)
            {
                builder.Append($" {name}");
            }

            if (parameter.HasDefaultValue)
            {
                builder.Append($" = {PrintUserFacing(parameter.DefaultValue)}");
            }

            return builder.ToString();
        }

        public static string PrintMethodSignature(this MethodInfo method, bool modifiers = false, bool arguments = true, bool returnType = true, bool name = true)
        {
            var builder = new StringBuilder();

            if (modifiers)
            {
                // Access modifiers.
                if (method.IsPublic)
                    builder.Append("public ");

                if (method.IsPrivate)
                    builder.Append("private ");

                if (method.IsFamilyAndAssembly)
                    builder.Append("private protected ");

                if (method.IsFamily)
                    builder.Append("protected ");

                if (method.IsFamilyOrAssembly)
                    builder.Append("protected internal ");

                if (method.IsAssembly)
                    builder.Append("internal ");

                if(method.IsStatic)
                    builder.Append("static ");

                if (method.IsAbstract && method.DeclaringType is {IsAbstract:true, IsInterface:false})
                    builder.Append("abstract ");
                else if(method.DeclaringType is {IsInterface:false})
                {
                    if (method.IsFinal)
                        builder.Append("sealed override ");
                    else if (method.IsVirtual)
                        builder.Append(method.Equals(method.GetBaseDefinition()) ? "virtual " : "override ");
                }
            }

            if (returnType && !method.IsConstructor)
                builder.Append($"{method.ReturnType.PrintTypeSignature()} ");

            if(name)
                builder.Append(method.Name);

            if (!arguments)
                return builder.ToString();

            if (method.IsGenericMethod)
                builder.Append($"<{string.Join(", ", method.GetGenericArguments().Select(PrintTypeSignature))}>");

            builder.Append($"({string.Join($", ", method.GetParameters().Select(PrintParameterSignature))})");

            return builder.ToString();
        }

        public static string PrintPropertySignature(this PropertyInfo property, bool modifiers = false, bool accessors = false)
        {
            var builder = new StringBuilder();

            builder.Append($"{property.PropertyType.PrintTypeSignature()} {property.Name}");

            if (accessors)
            {
                builder.Append(" { ");

                if (property.CanRead)
                    builder.Append($"{property.GetMethod!.PrintMethodSignature(modifiers, false, false, false)}get; ");

                if (property.CanWrite)
                    builder.Append($"{property.SetMethod!.PrintMethodSignature(modifiers, false, false, false)}set; ");

                builder.Append('}');

            }

            return builder.ToString();
        }

        public static string PrintFieldSignature(this FieldInfo field, bool modifiers = false)
        {
            var builder = new StringBuilder();

            if (modifiers)
            {
                // Access modifiers. Hmm... Déjà vu.
                if (field.IsPublic)
                    builder.Append("public ");

                if (field.IsPrivate)
                    builder.Append("private ");

                if (field.IsFamilyAndAssembly)
                    builder.Append("private protected ");

                if (field.IsFamily)
                    builder.Append("protected ");

                if (field.IsFamilyOrAssembly)
                    builder.Append("protected internal ");

                if (field.IsAssembly)
                    builder.Append("internal ");

                if(field.IsStatic)
                    builder.Append("static ");
            }

            builder.Append($"{field.FieldType.PrintTypeSignature()} {field.Name}");

            return builder.ToString();
        }

        private static readonly IReadOnlyDictionary<Type, string> TypeShortHand = new Dictionary<Type, string>()
        {
            // ReSharper disable BuiltInTypeReferenceStyle
            {typeof(void), "void"},
            {typeof(Object), "object"},
            {typeof(Boolean), "bool"},
            {typeof(Byte), "byte"},
            {typeof(Char), "char"},
            {typeof(Decimal), "decimal"},
            {typeof(Double), "double"},
            {typeof(Single), "float"},
            {typeof(Int32), "int"},
            {typeof(Int64), "long"},
            {typeof(SByte), "sbyte"},
            {typeof(Int16), "short"},
            {typeof(String), "string"},
            {typeof(UInt32), "uint"},
            {typeof(UInt64), "ulong"},
            {typeof(UInt16), "ushort"},
            // ReSharper restore BuiltInTypeReferenceStyle
        };
    }
}
