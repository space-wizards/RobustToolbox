using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace Robust.Shared.ContentPack
{
    internal sealed partial class AssemblyTypeChecker
    {
        internal abstract class MType
        {
            public virtual IEnumerable<MType> GetUsedTypes()
            {
                return Array.Empty<MType>();
            }

            public virtual bool WhitelistEquals(MType other)
            {
                return false;
            }

            public virtual bool IsCoreTypeDefined()
            {
                return false;
            }
        }

        internal abstract class MMemberRef
        {
            public readonly MType ParentType;
            public readonly string Name;

            protected MMemberRef(MType parentType, string name)
            {
                ParentType = parentType;
                Name = name;
            }
        }

        internal sealed class MMemberRefMethod : MMemberRef
        {
            public readonly MType ReturnType;
            public readonly int GenericParameterCount;
            public readonly ImmutableArray<MType> ParameterTypes;

            public MMemberRefMethod(MType parentType, string name, MType returnType,
                int genericParameterCount, ImmutableArray<MType> parameterTypes) : base(parentType, name)
            {
                ReturnType = returnType;
                GenericParameterCount = genericParameterCount;
                ParameterTypes = parameterTypes;
            }

            public override string ToString()
            {
                return $"{ReturnType} {ParentType}.{Name}({string.Join(", ", ParameterTypes)})";
            }
        }

        internal sealed class MMemberRefField : MMemberRef
        {
            public readonly MType FieldType;

            public MMemberRefField(MType parentType, string name, MType fieldType) : base(parentType, name)
            {
                FieldType = fieldType;
            }

            public override string ToString()
            {
                return $"{FieldType} {ParentType}.{Name}";
            }
        }

        internal sealed class MTypeParsed : MType
        {
            public readonly string FullName;
            public readonly MTypeParsed? NestedParent;

            public MTypeParsed(string fullName, MTypeParsed? nestedParent = null)
            {
                FullName = fullName;
                NestedParent = nestedParent;
            }

            public override string ToString()
            {
                return NestedParent != null ? $"{NestedParent}/{FullName}" : FullName;
            }

            public override bool WhitelistEquals(MType other)
            {
                switch (other)
                {
                    case MTypeParsed parsed:
                        if (NestedParent != null)
                        {
                            if (parsed.NestedParent == null || !NestedParent.WhitelistEquals(parsed.NestedParent))
                            {
                                return false;
                            }
                        }

                        return parsed.FullName == FullName;
                    case MTypeReferenced referenced:
                        if (NestedParent != null)
                        {
                            if (referenced.ResolutionScope is not MResScopeType parentRes ||
                                !NestedParent.WhitelistEquals(parentRes.Type))
                            {
                                return false;
                            }
                        }

                        var refFullName = referenced.Namespace == null
                            ? referenced.Name
                            : $"{referenced.Namespace}.{referenced.Name}";
                        return FullName == refFullName;
                    default:
                        return false;
                }
            }

            private bool Equals(MTypeParsed other)
            {
                return FullName == other.FullName;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is MTypeParsed other && Equals(other);
            }

            public override int GetHashCode()
            {
                return FullName.GetHashCode();
            }
        }

        internal sealed class MTypePrimitive : MType
        {
            public readonly PrimitiveTypeCode TypeCode;

            public MTypePrimitive(PrimitiveTypeCode typeCode)
            {
                TypeCode = typeCode;
            }

            public override string ToString()
            {
                return TypeCode switch
                {
                    PrimitiveTypeCode.Void => "void",
                    PrimitiveTypeCode.Boolean => "bool",
                    PrimitiveTypeCode.Char => "char",
                    PrimitiveTypeCode.SByte => "int8",
                    PrimitiveTypeCode.Byte => "unsigned int8",
                    PrimitiveTypeCode.Int16 => "int16",
                    PrimitiveTypeCode.UInt16 => "unsigned int16",
                    PrimitiveTypeCode.Int32 => "int32",
                    PrimitiveTypeCode.UInt32 => "unsigned int32",
                    PrimitiveTypeCode.Int64 => "int64",
                    PrimitiveTypeCode.UInt64 => "unsigned int64",
                    PrimitiveTypeCode.Single => "float32",
                    PrimitiveTypeCode.Double => "float64",
                    PrimitiveTypeCode.String => "string",
                    PrimitiveTypeCode.TypedReference => "typedref",
                    PrimitiveTypeCode.IntPtr => "native int",
                    PrimitiveTypeCode.UIntPtr => "unsigned native int",
                    PrimitiveTypeCode.Object => "object",
                    _ => "???"
                };
            }

            public override bool Equals(object? obj)
            {
                return obj is MTypePrimitive prim && prim.TypeCode == TypeCode;
            }

            public override int GetHashCode()
            {
                return (int) TypeCode;
            }

            public override bool WhitelistEquals(MType other)
            {
                return Equals(other);
            }
        }

        internal sealed class MTypeSZArray : MType
        {
            public readonly MType ElementType;

            public MTypeSZArray(MType elementType)
            {
                ElementType = elementType;
            }

            public override IEnumerable<MType> GetUsedTypes()
            {
                return new[] {ElementType};
            }

            public override string ToString()
            {
                return $"{ElementType}[]";
            }

            public override bool WhitelistEquals(MType other)
            {
                return other is MTypeSZArray arr && ElementType.WhitelistEquals(arr.ElementType);
            }
        }

        internal sealed class MTypeArray : MType
        {
            public readonly MType ElementType;
            public readonly ArrayShape Shape;

            public MTypeArray(MType elementType, ArrayShape shape)
            {
                ElementType = elementType;
                Shape = shape;
            }

            public override IEnumerable<MType> GetUsedTypes()
            {
                return new[] {ElementType};
            }

            public override string ToString()
            {
                return $"{ElementType}[TODO]";
            }

            public override bool WhitelistEquals(MType other)
            {
                return other is MTypeArray arr && ShapesEqual(Shape, arr.Shape) && ElementType.WhitelistEquals(arr);
            }

            private static bool ShapesEqual(in ArrayShape a, in ArrayShape b)
            {
                return a.Rank == b.Rank && a.LowerBounds.SequenceEqual(b.LowerBounds) && a.Sizes.SequenceEqual(b.Sizes);
            }

            public override bool IsCoreTypeDefined()
            {
                return ElementType.IsCoreTypeDefined();
            }
        }

        internal sealed class MTypeByRef : MType
        {
            public readonly MType ElementType;

            public MTypeByRef(MType elementType)
            {
                ElementType = elementType;
            }

            public override IEnumerable<MType> GetUsedTypes()
            {
                return new[] {ElementType};
            }

            public override string ToString()
            {
                return $"{ElementType}&";
            }

            public override bool WhitelistEquals(MType other)
            {
                return other is MTypeByRef byRef && ElementType.WhitelistEquals(byRef.ElementType);
            }
        }

        internal sealed class MTypePointer : MType
        {
            public MType ElementType { get; }

            public MTypePointer(MType elementType)
            {
                ElementType = elementType;
            }

            public override IEnumerable<MType> GetUsedTypes()
            {
                return new[] {ElementType};
            }

            public override string ToString()
            {
                return $"{base.ToString()}*";
            }

            public override bool WhitelistEquals(MType other)
            {
                return other is MTypePointer ptr && ElementType.WhitelistEquals(ptr.ElementType);
            }
        }

        internal sealed class MTypeGeneric : MType
        {
            public MType GenericType { get; }
            public ImmutableArray<MType> TypeArguments { get; }

            public MTypeGeneric(MType genericType, ImmutableArray<MType> typeArguments)
            {
                GenericType = genericType;
                TypeArguments = typeArguments;
            }

            public override IEnumerable<MType> GetUsedTypes()
            {
                yield return GenericType;

                foreach (var typeArgument in TypeArguments)
                {
                    yield return typeArgument;
                }
            }

            public override string ToString()
            {
                return $"{GenericType}<{string.Join(", ", TypeArguments)}>";
            }

            public override bool WhitelistEquals(MType other)
            {
                if (!(other is MTypeGeneric generic))
                {
                    return false;
                }

                if (TypeArguments.Length != generic.TypeArguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < TypeArguments.Length; i++)
                {
                    var argA = TypeArguments[i];
                    var argB = generic.TypeArguments[i];

                    if (!argA.WhitelistEquals(argB))
                    {
                        return false;
                    }
                }

                return GenericType.WhitelistEquals(generic.GenericType);
            }

            public override bool IsCoreTypeDefined()
            {
                return GenericType.IsCoreTypeDefined();
            }

            private bool Equals(MTypeGeneric other)
            {
                return GenericType.Equals(other.GenericType) && TypeArguments.SequenceEqual(other.TypeArguments);
            }

            public override bool Equals(object? obj)
            {
                return obj is MTypeGeneric other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hc = new HashCode();
                hc.Add(GenericType);
                hc.Add(TypeArguments.Length);
                foreach (var typeArg in TypeArguments)
                {
                    hc.Add(typeArg);
                }

                return hc.ToHashCode();
            }
        }

        internal sealed class MTypeDefined : MType
        {
            public string Name { get; }
            public string? Namespace { get; }
            public MTypeDefined? Enclosing { get; }

            public MTypeDefined(string name, string? ns, MTypeDefined? enclosing)
            {
                Name = name;
                Namespace = ns;
                Enclosing = enclosing;
            }

            public override string ToString()
            {
                var name = Namespace != null ? $"{Namespace}.{Name}" : Name;

                if (Enclosing != null)
                {
                    return $"{Enclosing}/{name}";
                }

                return name;
            }

            public override bool IsCoreTypeDefined()
            {
                return true;
            }
        }

        internal sealed class MTypeReferenced : MType
        {
            public MResScope ResolutionScope { get; }
            public string Name { get; }
            public string? Namespace { get; }

            public MTypeReferenced(MResScope resolutionScope, string name, string? @namespace)
            {
                ResolutionScope = resolutionScope;
                Name = name;
                Namespace = @namespace;
            }

            public override string ToString()
            {
                if (Namespace == null)
                {
                    return $"{ResolutionScope}{Name}";
                }
                else
                {
                    return $"{ResolutionScope}{Namespace}.{Name}";
                }
            }

            public override bool WhitelistEquals(MType other)
            {
                return other switch
                {
                    MTypeParsed p => p.WhitelistEquals(this),
                    // TODO: ResolutionScope doesn't actually implement equals
                    // This is fine since we're not comparing these anywhere
                    MTypeReferenced r => r.Namespace == Namespace && r.Name == Name &&
                                         r.ResolutionScope.Equals(ResolutionScope),
                    _ => false
                };
            }
        }

        internal abstract class MResScope
        {
        }

        internal sealed class MResScopeType : MResScope
        {
            public MType Type { get; }

            public MResScopeType(MType type)
            {
                Type = type;
            }

            public override string ToString()
            {
                return $"{Type}/";
            }
        }

        internal sealed class MResScopeAssembly : MResScope
        {
            public string Name { get; }

            public MResScopeAssembly(string name)
            {
                Name = name;
            }

            public override string ToString()
            {
                return $"[{Name}]";
            }
        }

        internal sealed class MTypeGenericTypePlaceHolder : MType
        {
            public int Index { get; }

            public MTypeGenericTypePlaceHolder(int index)
            {
                Index = index;
            }

            public override string ToString()
            {
                return $"!{Index}";
            }

            private bool Equals(MTypeGenericTypePlaceHolder other)
            {
                return Index == other.Index;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is MTypeGenericTypePlaceHolder other && Equals(other);
            }

            public override bool WhitelistEquals(MType other)
            {
                return Equals(other);
            }

            public override int GetHashCode()
            {
                return Index;
            }
        }

        internal sealed class MTypeGenericMethodPlaceHolder : MType
        {
            public int Index { get; }

            public MTypeGenericMethodPlaceHolder(int index)
            {
                Index = index;
            }

            public override string ToString()
            {
                return $"!!{Index}";
            }

            private bool Equals(MTypeGenericMethodPlaceHolder other)
            {
                return Index == other.Index;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is MTypeGenericMethodPlaceHolder other && Equals(other);
            }

            public override bool WhitelistEquals(MType other)
            {
                return Equals(other);
            }

            public override int GetHashCode()
            {
                return Index;
            }
        }

        internal sealed class MTypeModified : MType
        {
            public MType UnmodifiedType { get; }
            public MType ModifierType { get; }
            public bool Required { get; }

            public MTypeModified(MType unmodifiedType, MType modifierType, bool required)
            {
                UnmodifiedType = unmodifiedType;
                ModifierType = modifierType;
                Required = required;
            }

            public override string ToString()
            {
                var modName = Required ? "modreq" : "modopt";
                return $"{UnmodifiedType} {modName}({ModifierType})";
            }

            public override bool WhitelistEquals(MType other)
            {
                // TODO: This is asymmetric shit.
                return UnmodifiedType.WhitelistEquals(other);
            }
        }
    }
}
