using System;
using System.Collections.Generic;

namespace Robust.Server.Bql
{
    public struct BqlQuerySelectorParsed
    {
        public List<object> Arguments;
        public string Token;
        public bool Inverted;

        public BqlQuerySelectorParsed(List<object> arguments, string token, bool inverted)
        {
            Arguments = arguments;
            Token = token;
            Inverted = inverted;
        }
    }
}
