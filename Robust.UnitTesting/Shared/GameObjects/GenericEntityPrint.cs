using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Robust.UnitTesting.Shared.GameObjects;

public sealed class GenericEntityPrint
{
    //[Test]
    public void Print()
    {
        // Using the test framework for things it was not meant for is my passion
        var i = 8;

        IEnumerable<string> Generics(int n, bool nullable, bool forceIncludeNumber = false)
        {
            for (var j = 1; j <= n; j++)
            {
                var jStr = n == 1 && !forceIncludeNumber ? string.Empty : j.ToString();
                yield return $"T{jStr}{(nullable ? "?" : string.Empty)}";
            }
        }

        IEnumerable<string> PartiallyNullableGenerics(int n, int notNullCount)
        {
            bool nullable;
            for (var j = 1; j <= n; j++)
            {
                nullable = j > notNullCount;
                var jStr = n == 1 ? string.Empty : j.ToString();
                yield return $"T{jStr}{(nullable ? "?" : string.Empty)}";
            }
        }

        var structs = new StringBuilder();
        var constraints = new StringBuilder();
        var fields = new StringBuilder();
        var parameters = new StringBuilder();
        var asserts = new StringBuilder();
        var assignments = new StringBuilder();
        var tupleParameters = new StringBuilder();
        var tupleAccess = new StringBuilder();
        var entityAccess = new StringBuilder();
        var entityNumberedAccess = new StringBuilder();
        var defaults = new StringBuilder();
        var compOperators = new StringBuilder();
        var deConstructorParameters = new StringBuilder();
        var deConstructorAccess = new StringBuilder();
        var partialTupleCasts = new StringBuilder();
        var partialEntityCasts = new StringBuilder();
        var entitySubCast = new StringBuilder();
        var castRegion = new StringBuilder();

        for (var j = 1; j <= i; j++)
        {
            constraints.Clear();
            fields.Clear();
            parameters.Clear();
            asserts.Clear();
            assignments.Clear();
            tupleParameters.Clear();
            tupleAccess.Clear();
            entityAccess.Clear();
            entityNumberedAccess.Clear();
            defaults.Clear();
            compOperators.Clear();
            deConstructorParameters.Clear();
            deConstructorAccess.Clear();
            partialTupleCasts.Clear();
            partialEntityCasts.Clear();
            entitySubCast.Clear();
            castRegion.Clear();

            var generics = string.Join(", ", Generics(j, false));
            var nullableGenerics = string.Join(", ", Generics(j, true));

            for (var k = 1; k <= j; k++)
            {
                var kStr = j == 1 ? string.Empty : k.ToString();
                fields.AppendLine($"    public T{kStr} Comp{kStr};");
                constraints.Append($"where T{kStr} : IComponent? ");
                parameters.Append($", T{kStr} comp{kStr}");
                asserts.AppendLine($"        DebugTools.AssertOwner(owner, comp{kStr});");
                assignments.AppendLine($"        Comp{kStr} = comp{kStr};");
                tupleParameters.Append($", T{kStr} Comp{kStr}");
                tupleAccess.Append($", tuple.Comp{kStr}");
                var suffix = (j >= 2 && k == 1) ? string.Empty : kStr;
                var prefix = (j >= 2 && k == 2) ? "1" : string.Empty;
                entityAccess.Append($"{prefix}, ent.Comp{suffix}");
                entityNumberedAccess.Append($", ent.Comp{kStr}");
                defaults.Append(", default");
                compOperators.AppendLine($$"""
                        public static implicit operator T{{kStr}}(Entity<{{generics}}> ent)
                        {
                            return ent.Comp{{kStr}};
                        }

                    """);
                deConstructorParameters.Append($", out T{kStr} comp{kStr}");
                deConstructorAccess.AppendLine($"        comp{kStr} = Comp{kStr};");

                if (k == j)
                    continue;

                // Cast a (EntityUid, T1) tuple to an Entity<T1, T2?>
                // We could also casts for going from a (Uid, T2) tuple to a Entity<T1?, T2> but once we get to 4 or
                // more components there are just too many combinations and I CBF writing the code to generate all those.
                var partiallyNullableGenerics = string.Join(", ", PartiallyNullableGenerics(j, k));
                var defaultArgs = string.Concat(Enumerable.Repeat(", default", j-k));
                partialTupleCasts.Append($$"""

                        public static implicit operator Entity<{{partiallyNullableGenerics}}>((EntityUid Owner{{tupleParameters}}) tuple)
                        {
                            return new Entity<{{partiallyNullableGenerics}}>(tuple.Owner{{tupleAccess}}{{defaultArgs}});
                        }

                    """);

                // Cast an Entity<T1> to an Entity<T1, T2?>
                // As with the tuple casts, we could in principle generate more here.
                var subGenerics = string.Join(", ", Generics(k, false, true));
                partialEntityCasts.Append($$"""

                        public static implicit operator Entity<{{partiallyNullableGenerics}}>(Entity<{{subGenerics}}> ent)
                        {
                            return new Entity<{{partiallyNullableGenerics}}>(ent.Owner{{entityAccess}}{{defaultArgs}});
                        }

                    """);

                // Cast an Entity<T1, T2> to an Entity<T1/2>
                entitySubCast.Append($$"""

                        public static implicit operator Entity<{{subGenerics}}>(Entity<{{generics}}> ent)
                        {
                            return new Entity<{{subGenerics}}>(ent.Owner{{entityNumberedAccess}});
                        }

                    """);
            }

            if (j == 2)
            {
                castRegion.Append($$"""
                    {{partialTupleCasts.ToString().TrimEnd()}}
                    {{partialEntityCasts.ToString().TrimEnd()}}
                    {{entitySubCast.ToString().TrimEnd()}}
                    """);
            }
            else if (j > 2)
            {
                castRegion.Append($$"""

                    #region Partial Tuple Casts
                    {{partialTupleCasts}}
                    #endregion

                    #region Partial Entity Casts
                    {{partialEntityCasts}}
                    #endregion

                    #region Entity Sub casts
                    {{entitySubCast}}
                    #endregion
                    """);
            }

            structs.Append($$"""
                [NotYamlSerializable]
                public record struct Entity<{{generics}}> : IFluentEntityUid, IAsType<EntityUid>
                    {{constraints.ToString().TrimEnd()}}
                {
                    public EntityUid Owner;
                {{fields.ToString().TrimEnd()}}

                    public Entity(EntityUid owner{{parameters}})
                    {
                {{asserts}}
                        Owner = owner;
                {{assignments.ToString().TrimEnd()}}
                    }

                    public static implicit operator Entity<{{generics}}>((EntityUid Owner{{tupleParameters}}) tuple)
                    {
                        return new Entity<{{generics}}>(tuple.Owner{{tupleAccess}});
                    }

                    public static implicit operator Entity<{{nullableGenerics}}>(EntityUid owner)
                    {
                        return new Entity<{{nullableGenerics}}>(owner{{defaults}});
                    }

                    public static implicit operator EntityUid(Entity<{{generics}}> ent)
                    {
                        return ent.Owner;
                    }

                {{compOperators.ToString().TrimEnd()}}

                    public readonly void Deconstruct(out EntityUid owner{{deConstructorParameters}})
                    {
                        owner = Owner;
                {{deConstructorAccess.ToString().TrimEnd()}}
                    }
                    {{castRegion}}

                    EntityUid IFluentEntityUid.FluentOwner => Owner;
                    public EntityUid AsType() => Owner;
                }


                """);
        }

        Console.WriteLine(structs);
    }
}
