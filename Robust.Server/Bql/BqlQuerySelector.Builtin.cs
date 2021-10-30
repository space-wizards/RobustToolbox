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

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var comp = (Type) arguments[0];
            return input.Where(x => entityManager.HasComponent(x, comp) ^ isInverted);
        }

        public override IEnumerable<EntityUid> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            if (isInverted)
            {
                return base.DoInitialSelection(arguments, isInverted, entityManager);
            }

            return entityManager.GetAllComponents((Type) arguments[0])
                .Select(x => x.OwnerUid);
        }
    }

    [RegisterBqlQuerySelector]
    public class NamedQuerySelector : BqlQuerySelector
    {
        public override string Token => "named";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var r = new Regex("^" + (string) arguments[0] + "$");
            return input.Where(e =>
            {
                if (entityManager.TryGetComponent<MetaDataComponent>(e, out var metaDataComponent))
                    return r.IsMatch(metaDataComponent.EntityName) ^ isInverted;
                return isInverted;
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class ParentedToQuerySelector : BqlQuerySelector
    {
        public override string Token => "parentedto";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.EntityId };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var uid = (EntityUid) arguments[0];
            return input.Where(e => (entityManager.TryGetComponent<ITransformComponent>(e, out var transform) &&
                                     transform.Parent?.OwnerUid == uid) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class RecursiveParentedToQuerySelector : BqlQuerySelector
    {
        public override string Token => "rparentedto";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.EntityId };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var uid = (EntityUid) arguments[0];
            return input.Where(e =>
            {
                if (!entityManager.TryGetComponent<ITransformComponent>(e, out var transform))
                    return isInverted;
                var cur = transform;
                while (cur.ParentUid != EntityUid.Invalid)
                {
                    if ((cur.ParentUid == uid) ^ isInverted)
                        return true;
                    if (cur.Parent is null)
                        return false;
                    cur = cur.Parent;
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

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            return input.SelectMany(e =>
            {
                return entityManager.TryGetComponent<ITransformComponent>(e, out var transform)
                    ? transform.Children.Select(y => y.OwnerUid)
                    : new List<EntityUid>();
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class RecursiveChildrenQuerySelector : BqlQuerySelector
    {
        public override string Token => "rchildren";

        public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments,
            bool isInverted, IEntityManager entityManager)
        {
            IEnumerable<EntityUid> toSearch = input;
            List<IEnumerable<EntityUid>> children = new List<IEnumerable<EntityUid>>();

            while (true)
            {
                var doing = toSearch.Where(entityManager.HasComponent<ITransformComponent>).Select(entityManager.GetComponent<ITransformComponent>).ToArray();
                var search = doing.SelectMany(x => x.Children.Select(y => y.Owner));
                if (!search.Any())
                    break;
                toSearch = doing.SelectMany(x => x.Children.Select(y => y.OwnerUid)).Where(x => x != EntityUid.Invalid);
                children.Add(doing.SelectMany(x => x.Children.Select(y => y.OwnerUid)).Where(x => x != EntityUid.Invalid));
            }

            return children.SelectMany(x => x);
        }
    }

    [RegisterBqlQuerySelector]
    public class ParentQuerySelector : BqlQuerySelector
    {
        public override string Token => "parent";

        public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            return input.Where(entityManager.HasComponent<ITransformComponent>)
                .Select(e => entityManager.GetComponent<ITransformComponent>(e).OwnerUid)
                .Distinct();
        }

        public override IEnumerable<EntityUid> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            return base.DoSelection(entityManager.EntityQuery<ITransformComponent>().Select(x => x.OwnerUid), arguments,
                isInverted, entityManager);
        }
    }

    [RegisterBqlQuerySelector]
    public class AboveQuerySelector : BqlQuerySelector
    {
        public override string Token => "above";

        public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.String };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            var tileTy = tileDefinitionManager[(string) arguments[0]];
            var entity = IoCManager.Resolve<IEntityManager>();
            var map = IoCManager.Resolve<IMapManager>();
            if (tileTy.TileId == 0)
            {
                return input.Where(e => entityManager.TryGetComponent<ITransformComponent>(e, out var transform) && (transform.Coordinates.GetGridId(entity) == GridId.Invalid) ^ isInverted);
            }
            else
            {
                return input.Where(e =>
                {
                    if (!entityManager.TryGetComponent<ITransformComponent>(e, out var transform)) return isInverted;

                    var gridId = transform.Coordinates.GetGridId(entity);
                    if (gridId == GridId.Invalid)
                        return isInverted;

                    var grid = map.GetGrid(gridId);
                    return (grid.GetTileRef(transform.Coordinates).Tile.TypeId == tileTy.TileId) ^ isInverted;

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

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var grid = new GridId((int) arguments[0]);
            return input.Where(e => (entityManager.TryGetComponent<ITransformComponent>(e, out var transform) && transform.GridID == grid) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    // ReSharper disable once InconsistentNaming the name is correct shut up
    public class OnMapQuerySelector : BqlQuerySelector
    {
        public override string Token => "onmap";

        public override QuerySelectorArgument[] Arguments => new [] { QuerySelectorArgument.Integer };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var map = new MapId((int) arguments[0]);
            return input.Where(e => (entityManager.TryGetComponent<ITransformComponent>(e, out var transform) && transform.MapID == map) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class PrototypedQuerySelector : BqlQuerySelector
    {
        public override string Token => "prototyped";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var name = (string) arguments[0];
            return input.Where(e => (entityManager.TryGetComponent<MetaDataComponent>(e, out var metaData) && metaData.EntityPrototype?.ID == name) ^ isInverted);
        }
    }

    [RegisterBqlQuerySelector]
    public class RecursivePrototypedQuerySelector : BqlQuerySelector
    {
        public override string Token => "rprototyped";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.String };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var name = (string) arguments[0];
            return input.Where(e =>
            {
                if (!entityManager.TryGetComponent<MetaDataComponent>(e, out var metaData))
                    return isInverted;
                if ((metaData.EntityPrototype?.ID == name) ^ isInverted)
                    return true;

                return (metaData.EntityPrototype?.Parent == name) ^ isInverted; // Damn, can't actually do recursive check here.
            });
        }
    }

    [RegisterBqlQuerySelector]
    public class SelectQuerySelector : BqlQuerySelector
    {
        public override string Token => "select";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.Integer | QuerySelectorArgument.Percentage };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            if (arguments[0] is int)
            {
                var inp = input.OrderBy(_ => Guid.NewGuid()).ToArray();
                var taken = (int) arguments[0];

                if (isInverted)
                    taken = Math.Max(0, inp.Length - taken);

                return inp.Take(taken);
            }

            var enumerable = input.OrderBy(_ => Guid.NewGuid()).ToArray();
            var amount = isInverted
                ? (int) Math.Floor(enumerable.Length * Math.Clamp(1 - (double) arguments[0], 0, 1))
                : (int) Math.Floor(enumerable.Length * Math.Clamp((double) arguments[0], 0, 1));
            return enumerable.Take(amount);
        }
    }

    [RegisterBqlQuerySelector]
    public class NearQuerySelector : BqlQuerySelector
    {
        public override string Token => "near";

        public override QuerySelectorArgument[] Arguments => new []{ QuerySelectorArgument.Float };

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            var radius = (float)(double)arguments[0];
            var entityLookup = IoCManager.Resolve<IEntityLookup>();

            //BUG: GetEntitiesInRange effectively uses manhattan distance. This is not intended, near is supposed to be circular.
            return input.Where(entityManager.HasComponent<ITransformComponent>)
                .SelectMany(e =>
                    entityLookup.GetEntitiesInRange(entityManager.GetComponent<ITransformComponent>(e).Coordinates,
                        radius))
                .Select(x => x.Uid) // Sloth's fault.
                .Distinct();
        }
    }

    [RegisterBqlQuerySelector]
    // ReSharper disable once InconsistentNaming the name is correct shut up
    public class anchoredQuerySelector : BqlQuerySelector
    {
        public override string Token => "anchored";

        public override QuerySelectorArgument[] Arguments => Array.Empty<QuerySelectorArgument>();

        public override IEnumerable<EntityUid> DoSelection(IEnumerable<EntityUid> input, IReadOnlyList<object> arguments, bool isInverted, IEntityManager entityManager)
        {
            return input.Where(e => (entityManager.TryGetComponent<ITransformComponent>(e, out var transform) && transform.Anchored) ^ isInverted);
        }
    }
}
