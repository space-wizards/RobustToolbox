using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class FloatTypeParser : NumberBaseTypeParser<float>
{
}

internal sealed class DoubleTypeParser : NumberBaseTypeParser<double>
{
}

internal sealed class DecimalTypeParser : NumberBaseTypeParser<decimal>
{
}

internal sealed class HalfTypeParser : NumberBaseTypeParser<Half>
{
}

internal sealed class ComplexTypeParser : NumberBaseTypeParser<Complex>
{
}
