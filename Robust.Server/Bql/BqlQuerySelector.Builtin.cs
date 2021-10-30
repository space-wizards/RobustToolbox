using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

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

        public override IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
        {
            if (isInverted)
            {
                return base.DoInitialSelection(arguments, isInverted);
            }

            return IoCManager.Resolve<IEntityManager>().GetAllComponents((Type) arguments[0])
                .Select(x => x.Owner);
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
            return input.Where(e => (e.Transform.ParentUid == uid) ^ isInverted);
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
                    if ((cur.Transform.ParentUid == uid) ^ isInverted)
                        return true;
                    cur = cur.Transform.Parent.Owner;
                }

                return false;
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class ChildrenQuerySelector : BqlQuerySelector
    {
        public override string Token => "children";

        public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            return input.SelectMany(x => x.Transform.Children.Select(y => y.Owner));
        }
    }

    [RegisterBqlQuerySelector]
    public class ParentQuerySelector : BqlQuerySelector
    {
        public override string Token => "parent";

        public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            return input.Where(x => x.Transform.Parent is not null).Select(x => x.Transform.Parent!.Owner).Distinct();
        }
    }

    [RegisterBqlQuerySelector]
    public class AboveQuerySelector : BqlQuerySelector
    {
        public override string Token => "above";

        public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.String };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var tileTy = tileDefinitionManager[(string) arguments[0]];
            var entity = IoCManager.Resolve<IEntityManager>();
            var map = IoCManager.Resolve<IMapManager>();
            if (tileTy.TileId == 0)
            {
                return input.Where(x => (x.Transform.Coordinates.GetGridId(entity) != GridId.Invalid) ^ isInverted);
            }
            else
            {
                return input.Where(x => x.Transform.Coordinates.GetGridId(entity) != GridId.Invalid).Where(x =>
                {
                    var gridId = x.Transform.Coordinates.GetGridId(entity);
                    var grid = map.GetGrid(gridId);
                    return (grid.GetTileRef(x.Transform.Coordinates).Tile.TypeId == tileTy.TileId) ^ isInverted;
                });
            }
        }
    }

    [RegisterBqlQuerySelector]
    // ReSharper disable once InconsistentNaming the name is correct shut up
    public class OnGridQuerySelector : BqlQuerySelector
    {
        public override string Token => "ongrid";

        public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.Integer };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var grid = new GridId((int) arguments[0]);
            return input.Where(x => (x.Transform.GridID == grid) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    // ReSharper disable once InconsistentNaming the name is correct shut up
    public class OnMapQuerySelector : BqlQuerySelector
    {
        public override string Token => "onmap";

        public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.Integer };

        public override IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            var map = new MapId((int) arguments[0]);
            return input.Where(x => (x.Transform.MapID == map) ^ isInverted);
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
            return input.Where(e => (e.Prototype?.ID == name) ^ isInverted);
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
                if ((e.Prototype?.ID == name) ^ isInverted)
                    return true;

                return (e.Prototype?.Parent == name) ^ isInverted; // Damn, can't actually do recursive check here.
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
