using System;
using System.Collections.Generic;
using System.Text;

using Robust.Shared.Reflection;

namespace Robust.Analyzer
{
    internal class AnalyzerReflectionManager : ReflectionManager
    {
        protected override IEnumerable<string> TypePrefixes => new string[] { "Robust.Shared." };
    }
}
