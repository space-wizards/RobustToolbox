using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Server.Bql
{
    public interface IBqlQueryManager
    {
        public (IEnumerable<IEntity>, string) SimpleParseAndExecute(string query);
        void DoAutoRegistrations();
    }
}
