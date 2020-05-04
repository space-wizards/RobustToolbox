using System;
using System.Collections.Generic;
using System.Text;

using Robust.Shared.Reflection;

namespace Robust.Analyzer
{
    internal class AnalyzerReflectionManager : ReflectionManager
    {
        private readonly string[] _typePrefixes;

        protected override IEnumerable<string> TypePrefixes => _typePrefixes;

        public AnalyzerReflectionManager(string[] typePrefixes)
        {
            _typePrefixes = typePrefixes;
        }
    }
}
