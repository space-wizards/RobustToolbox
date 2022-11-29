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
            var parsed = SimpleQuery.Parse(query);
            if (parsed.Success)
            {
                var selectors = parsed.Value.Item1.ToArray();
                if (selectors.Length == 0)
                {
                    return (_entityManager.GetEntities(), parsed.Value.Item2);
                }

                var entities = _queriesByToken[selectors[0].Token]
                    .DoInitialSelection(selectors[0].Arguments, selectors[0].Inverted, _entityManager);

                foreach (var sel in selectors[1..])
                {
                    entities = _queriesByToken[sel.Token].DoSelection(entities, sel.Arguments, sel.Inverted, _entityManager);
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
