using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.UnitTesting.Shared.GameObjects;

public sealed class GenericEntityPrint
{
    // [Test]
    public void Print()
    {
        // Using the test framework for things it was not meant for is my passion
        var i = 8;

        IEnumerable<string> Generics(int n, bool nullable)
        {
            for (var j = 1; j <= n; j++)
            {
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
        var defaults = new StringBuilder();
        var compOperators = new StringBuilder();
        var deConstructorParameters = new StringBuilder();
        var deConstructorAccess = new StringBuilder();

        for (var j = 1; j <= i; j++)
        {
            constraints.Clear();
            fields.Clear();
            parameters.Clear();
            asserts.Clear();
            assignments.Clear();
            tupleParameters.Clear();
            tupleAccess.Clear();
            defaults.Clear();
            compOperators.Clear();
            deConstructorParameters.Clear();
            deConstructorAccess.Clear();

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
                defaults.Append(", default");
                compOperators.AppendLine($$"""
                        public static implicit operator T{{kStr}}(Entity<{{generics}}> ent)
                        {
                            return ent.Comp{{kStr}};
                        }

                    """);
                deConstructorParameters.Append($", out T{kStr} comp{kStr}");
                deConstructorAccess.AppendLine($"        comp{kStr} = Comp{kStr};");
            }

            structs.Append($$"""
                public record struct Entity<{{generics}}>
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
                }


                """);
        }

        Console.WriteLine(structs);
    }
}
