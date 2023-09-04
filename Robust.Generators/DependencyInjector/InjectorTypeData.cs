using System;
using System.Collections.Immutable;

namespace Robust.Generators.DependencyInjector;

public sealed class InjectorTypeData : IEquatable<InjectorTypeData>
{
    public string? Namespace { get; }
    public string FileName { get; }
    public PartialTypeDeclarationData TypeDeclaration { get; }
    public ImmutableArray<PartialTypeDeclarationData> ContainingDeclarations { get; }
    public bool BaseTypeIsInjector { get; }
    public ImmutableArray<DependencyFieldData> Fields { get; }
    public bool IsSealed { get; }

    public InjectorTypeData(string fileName,
        bool baseTypeIsInjector,
        ImmutableArray<DependencyFieldData> fields,
        string? ns,
        PartialTypeDeclarationData typeDeclaration,
        ImmutableArray<PartialTypeDeclarationData> containingDeclarations,
        bool isSealed)
    {
        FileName = fileName;
        BaseTypeIsInjector = baseTypeIsInjector;
        Fields = fields;
        Namespace = ns;
        TypeDeclaration = typeDeclaration;
        ContainingDeclarations = containingDeclarations;
        IsSealed = isSealed;
    }

    public bool Equals(InjectorTypeData? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Namespace == other.Namespace
               && FileName == other.FileName
               && TypeDeclaration.Equals(other.TypeDeclaration)
               && ModelHelper.ArrayEquals(ContainingDeclarations, other.ContainingDeclarations)
               && BaseTypeIsInjector == other.BaseTypeIsInjector
               && ModelHelper.ArrayEquals(Fields, other.Fields)
               && IsSealed == other.IsSealed;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is InjectorTypeData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ FileName.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeDeclaration.GetHashCode();
            hashCode = (hashCode * 397) ^ ModelHelper.ArrayHashCode(ContainingDeclarations);
            hashCode = (hashCode * 397) ^ BaseTypeIsInjector.GetHashCode();
            hashCode = (hashCode * 397) ^ ModelHelper.ArrayHashCode(Fields);
            hashCode = (hashCode * 397) ^ IsSealed.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(InjectorTypeData? left, InjectorTypeData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InjectorTypeData? left, InjectorTypeData? right)
    {
        return !Equals(left, right);
    }
}

public sealed class DependencyFieldData : IEquatable<DependencyFieldData>
{
    public string Name { get; }
    public string TypeName { get; }

    public DependencyFieldData(string name, string typeName)
    {
        Name = name;
        TypeName = typeName;
    }

    public bool Equals(DependencyFieldData? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name && TypeName == other.TypeName;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DependencyFieldData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ TypeName.GetHashCode();
        }
    }

    public static bool operator ==(DependencyFieldData? left, DependencyFieldData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(DependencyFieldData? left, DependencyFieldData? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Necessary data to reconstruct a partial type declaration for a type.
/// </summary>
public sealed class PartialTypeDeclarationData : IEquatable<PartialTypeDeclarationData>
{
    public string Name { get; }
    public PartialTypeKind Kind { get; }

    public PartialTypeDeclarationData(string name, PartialTypeKind kind)
    {
        Name = name;
        Kind = kind;
    }

    public bool Equals(PartialTypeDeclarationData? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name && Kind == other.Kind;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is PartialTypeDeclarationData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ (int)Kind;
        }
    }

    public static bool operator ==(PartialTypeDeclarationData? left, PartialTypeDeclarationData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PartialTypeDeclarationData? left, PartialTypeDeclarationData? right)
    {
        return !Equals(left, right);
    }
}

public enum PartialTypeKind
{
    Interface,
    Class,
    Struct,
    Record,
    RecordStruct
}
