using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Bql
{
    public partial class BqlQueryManager
    {
        public (IEnumerable<EntityUid>, string) SimpleParseAndExecute(string query)
        {
            var parsed = _simpleQuery.Parse(query);
            if (parsed.Success)
            {
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var selectors = parsed.Value.Item1.ToArray();
                if (selectors.Length == 0)
                {
                    return (entityManager.GetEntityUids(), parsed.Value.Item2);
                }

                var entities = _queriesByToken[selectors[0].Token]
                    .DoInitialSelection(selectors[0].Arguments, selectors[0].Inverted, entityManager);

                foreach (var sel in selectors[1..])
                {
                    entities = _queriesByToken[sel.Token].DoSelection(entities, sel.Arguments, sel.Inverted, entityManager);
                }

                return (entities, parsed.Value.Item2);
            }
            else
            {
                throw new Exception(parsed.Error!.RenderErrorMessage());
            }
        }
    }
}
