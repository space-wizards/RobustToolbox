using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.GameObjects;

namespace Robust.Server.Bql
{
    [RegisterBqlQuerySelector]
    public class WithQuerySelector : BqlQuerySelector
    {
        public override string Token => "with";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.Component };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var comp = (Type) arguments[0];
            return input.Where(x => x.HasComponent(comp) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class NamedQuerySelector : BqlQuerySelector
    {
        public override string Token => "named";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var r = new Regex("^" + (string) arguments[0] + "$");
            return input.Where(e => r.IsMatch(e.Name) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class ParentedToQuerySelector : BqlQuerySelector
    {
        public override string Token => "parentedto";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.EntityId };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var uid = (EntityUid) arguments[0];
            return input.Where(e => e.Transform.ParentUid == uid ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class RecursiveParentedToQuerySelector : BqlQuerySelector
    {
        public override string Token => "rparentedto";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.EntityId };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var uid = (EntityUid) arguments[0];
            return input.Where(e =>
            {
                var cur = e;
                while (cur.Transform.Parent is not null)
                {
                    if (cur.Transform.ParentUid == uid ^ isInverted)
                        return true;
                    cur = cur.Transform.Parent.Owner;
                }

                return false;
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class PrototypedQuerySelector : BqlQuerySelector
    {
        public override string Token => "prototyped";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var name = (string) arguments[0];
            return input.Where(e => e.Prototype?.Name == name ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class RecursivePrototypedQuerySelector : BqlQuerySelector
    {
        public override string Token => "rprototyped";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var name = (string) arguments[0];
            return input.Where(e =>
            {
                if (e.Prototype?.Name == name ^ isInverted)
                    return true;

                return e.Prototype?.Parent == name ^ isInverted; // Damn, can't actually do recursive check here.
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class SelectQuerySelector : BqlQuerySelector
    {
        public override string Token => "select";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.Integer | QuerySelectorArgument.Percentage };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            if (arguments[0] is int)
            {
                return input.OrderBy(a => Guid.NewGuid()).Take((int) arguments[0]);
            }

            var enumerable = input.OrderBy(a => Guid.NewGuid()).ToArray();
            var amount = (int)Math.Floor(enumerable.Length * (double)arguments[0]);
            return enumerable.Take(amount);
        }
    }
}
