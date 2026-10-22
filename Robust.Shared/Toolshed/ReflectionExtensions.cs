using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

// TODO: Audit this for sandboxability and expose some of these to content.
internal static class ReflectionExtensions
{
    public static bool CanBeNull(this Type t)
    {
        return !t.IsValueType || t.IsGenericType(typeof(Nullable<>));
    }

    public static bool CanBeEmpty(this Type t)
    {
        return t.CanBeNull() || t.IsGenericType(typeof(IEnumerable<>));
    }

    public static bool IsGenericType(this Type t, Type genericType)
    {
        return t.IsGenericType && t.GetGenericTypeDefinition() == genericType;
    }

    public static IEnumerable<Type> GetVariants(this Type t, ToolshedManager toolshed)
    {
        var args = t.GetGenericArguments();
        var generic = t.GetGenericTypeDefinition();
        var genericArgs = generic.GetGenericArguments();
        var variantCount = genericArgs.Count(x => (x.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) != 0);

        if (variantCount > 1)
        {
            throw new NotImplementedException("I swear to god I am NOT supporting more than one variant type parameter. Absolutely no combinatorial explosions in this house, factorials can go home.");
        }

        yield return t;

        if (variantCount < 1)
        {
            yield break;
        }

        var variant = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if ((genericArgs[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) != 0)
            {
                variant = i;
                break;
            }
        }

        var newArgs = (Type[]) args.Clone();

        foreach (var type in toolshed.AllSteppedTypes(args[variant], false))
        {
            newArgs[variant] = type;
            yield return generic.MakeGenericType(newArgs);
        }
    }

    public static Type StepDownConstraints(this Type t)
    {
        if (!t.IsGenericType || t.IsGenericTypeDefinition)
            return t;

        var oldArgs = t.GenericTypeArguments;
        var newArgs = new Type[oldArgs.Length];

        for (var i = 0; i < oldArgs.Length; i++)
        {
            if (oldArgs[i].IsGenericType)
                newArgs[i] = oldArgs[i].GetGenericTypeDefinition();
            else
                newArgs[i] = oldArgs[i];
        }

        return t.GetGenericTypeDefinition().MakeGenericType(newArgs);
    }

    public static bool HasGenericParent(this Type type, Type parent)
    {
        DebugTools.Assert(parent.IsGenericType);
        var t = type;
        while (t != null)
        {
            if (t.IsGenericType(parent))
                return true;

            t = t.BaseType;
        }

        return false;
    }

    public static bool IsValueRef(this Type type)
    {
        return type.HasGenericParent(typeof(ValueRef<>));
    }

    public static bool IsCustomParser(this Type type)
    {
        return type.HasGenericParent(typeof(CustomTypeParser<>));
    }

    public static bool IsParser(this Type type)
    {
        return type.HasGenericParent(typeof(TypeParser<>));
    }

    public static bool IsCommandArgument(this ParameterInfo param)
    {
        if (param.HasCustomAttribute<CommandArgumentAttribute>())
            return true;

        if (param.HasCustomAttribute<CommandInvertedAttribute>())
            return false;

        if (param.HasCustomAttribute<PipedArgumentAttribute>())
            return false;

        if (param.HasCustomAttribute<CommandInvocationContextAttribute>())
            return false;

        return param.ParameterType != typeof(IInvocationContext);
    }

    public static string PrettyName(this Type type)
    {
        var name = type.Name;

        if (type.IsGenericParameter)
            return type.ToString();

        if (type.DeclaringType is not null)
        {
            name = $"{PrettyName(type.DeclaringType!)}.{type.Name}";
        }

        if (type.GetGenericArguments().Length == 0)
        {
            return name;
        }

        if (!name.Contains('`'))
            return name + "<>";

        var genericArguments = type.GetGenericArguments();
        var exactName = name.Substring(0, name.IndexOf('`', StringComparison.InvariantCulture));
        return exactName + "<" + string.Join(",", genericArguments.Select(PrettyName)) + ">";
    }

    public static ParameterInfo? ConsoleGetPipedArgument(this MethodInfo method)
    {
        return method.GetParameters().SingleOrDefault(x => x.HasCustomAttribute<PipedArgumentAttribute>());
    }

    public static bool ConsoleHasInvertedArgument(this MethodInfo method)
    {
        return method.GetParameters().Any(x => x.HasCustomAttribute<CommandInvertedAttribute>());
    }

    public static Expression CreateEmptyExpr(this Type t)
    {
        if (!t.CanBeEmpty())
            throw new TypeArgumentException();

        if (t.IsGenericType(typeof(IEnumerable<>)))
        {
            var array = Array.CreateInstance(t.GetGenericArguments().First(), 0);
            return Expression.Constant(array, t);
        }

        if (t.CanBeNull())
        {
            if (Nullable.GetUnderlyingType(t) is not null)
                return Expression.Constant(t.GetConstructor(BindingFlags.CreateInstance, Array.Empty<Type>())!.Invoke(null, null), t);

            return Expression.Constant(null, t);
        }

        throw new NotImplementedException();
    }

    // IEnumerable<EntityUid> ^ IEnumerable<T> -> EntityUid
    // List<T> ^ IEnumerable<T> -> EntityUid
    // T[] ^ IEnumerable<T> -> EntityUid
    public static Type Intersect(this Type left, Type right)
    {
        // TODO TOOLSHED implement this properly.
        // AAAAHhhhhh
        // this is all sphagetti and needs fixing.
        // I'm just bodging a fix for now that makes it treat arrays as equivalent to a list.
        if (left.IsArray)
            return Intersect(typeof(List<>).MakeGenericType(left.GetElementType()!), right);

        if (right.IsArray)
            return Intersect(left, typeof(List<>).MakeGenericType(right.GetElementType()!));

        if (!left.IsGenericType)
            return left;

        if (!right.IsGenericType)
            return left;

        var leftGen = left.GetGenericTypeDefinition();
        var rightGen = right.GetGenericTypeDefinition();
        var leftArgs = left.GetGenericArguments();

        // TODO TOOLSHED implement this properly.
        // Currently this only recurses through the first generic argument.

        if (leftGen == rightGen)
            return Intersect(leftArgs.First(), right.GenericTypeArguments.First());

        return Intersect(leftArgs.First(), right);
    }

    public static void DumpGenericInfo(this Type t)
    {
        Logger.Debug($"Info for {t.PrettyName()}");
        Logger.Debug(
            $"GP {t.IsGenericParameter} | MP {t.IsGenericMethodParameter} | TP {t.IsGenericTypeParameter} | DEF {t.IsGenericTypeDefinition} | TY {t.IsGenericType} | CON {t.IsConstructedGenericType}");
        if (t.IsGenericParameter)
            Logger.Debug($"CONSTRAINTS: {string.Join(", ", t.GetGenericParameterConstraints().Select(PrettyName))}");
        if (!t.IsGenericTypeDefinition && IsGenericRelated(t) && t.IsGenericType)
            DumpGenericInfo(t.GetGenericTypeDefinition());
        foreach (var p in t.GetGenericArguments())
        {
            DumpGenericInfo(p);
        }
    }

    public static bool IsAssignableToGeneric(this Type left, Type right, ToolshedManager toolshed, bool recursiveDescent = true)
    {
        return left.IntersectWithGeneric(right, toolshed, recursiveDescent) is not null;
    }

    /// <summary>
    /// Hopefully allows to figure out all the relevant type arguments when intersecting concrete with a generic one. Returns null if no intersection is possible
    /// Pseudocode: <c>IEnumerable&lt;EntityUid&gt; ^ IEnumerable&lt;T&gt; -&gt; [EntityUid]</c>
    /// </summary>
    public static Type[]? IntersectWithGeneric(this Type left, Type right, ToolshedManager toolshed, bool recursiveDescent)
    {
        if (left.IsAssignableTo(right))
            return [left];

        if (right.IsInterface && !left.IsInterface)
        {
            foreach (var i in left.GetInterfaces())
            {
                if (right.GetMostGenericPossible() != i.GetMostGenericPossible())
                    continue;
                if (right.IntersectWithGeneric(i, toolshed, recursiveDescent) is var outType && outType is not null)
                    return outType;
            }
        }

        if (left.Constructable() && right.IsGenericParameter)
        {
            // TODO: We need a constraint solver and a general overhaul of how toolshed constructs implementations.
            return [left];
        }

        if (left.IsGenericType && right.IsGenericType && left.GenericTypeArguments.Length == right.GenericTypeArguments.Length)
        {
            var equal = left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition();

            if (!equal)
                goto next;

            Type[]? res = null;
            foreach (var (leftTy, rightTy) in left.GenericTypeArguments.Zip(right.GenericTypeArguments))
            {
                if (leftTy.IntersectWithGeneric(rightTy, toolshed, false) is var outType && outType is not null)
                    res = [ .. res ?? [], .. outType ];
            }

            return res;
        }

        next:
        if (recursiveDescent)
        {
            foreach (var leftSubTy in toolshed.AllSteppedTypes(left))
            {
                if (leftSubTy.IntersectWithGeneric(right, toolshed, false) is var outType && outType is not null)
                {
                    return outType;
                }
            }
        }

        return null;
    }

    public static bool IsGenericRelated(this Type t)
    {
        return t.IsGenericParameter | t.IsGenericType | t.IsGenericMethodParameter | t.IsGenericTypeDefinition | t.IsConstructedGenericType | t.IsGenericTypeParameter;
    }

    public static bool Constructable(this Type t)
    {
        if (!IsGenericRelated(t))
            return true;

        if (!t.IsGenericType || !t.IsConstructedGenericType)
            return false;

        var r = true;

        foreach (var arg in t.GetGenericArguments())
        {
            r &= Constructable(arg);
        }

        return r;
    }

    public static PropertyInfo? FindIndexerProperty(
        this Type type)
    {
        var defaultPropertyAttribute = type.GetCustomAttributes<DefaultMemberAttribute>().FirstOrDefault();

        return defaultPropertyAttribute == null
            ? null
            : type.GetRuntimeProperties()
                .FirstOrDefault(
                    pi =>
                        pi.Name == defaultPropertyAttribute.MemberName
                        && pi.IsIndexerProperty()
                        && pi.SetMethod?.GetParameters() is { } parameters
                        && parameters.Length == 2
                        && parameters[0].ParameterType == typeof(string));
    }

    public static bool IsIndexerProperty(this PropertyInfo propertyInfo)
    {
        var indexParams = propertyInfo.GetIndexParameters();
        return indexParams.Length == 1
               && indexParams[0].ParameterType == typeof(string);
    }

    public static Type GetMostGenericPossible(this Type t)
    {
        if (!t.IsGenericType)
            return t;

        return t.GetGenericTypeDefinition();
    }
}
