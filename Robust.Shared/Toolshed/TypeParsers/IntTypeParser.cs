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

internal sealed class IntTypeParser : NumberBaseTypeParser<int>
{
}

internal sealed class UIntTypeParser : NumberBaseTypeParser<uint>
{
}

internal sealed class ByteTypeParser : NumberBaseTypeParser<byte>
{
}

internal sealed class SByteTypeParser : NumberBaseTypeParser<sbyte>
{
}

internal sealed class ShortTypeParser : NumberBaseTypeParser<short>
{
}

internal sealed class UShortTypeParser : NumberBaseTypeParser<ushort>
{
}

internal sealed class LongTypeParser : NumberBaseTypeParser<long>
{
}

internal sealed class ULongTypeParser : NumberBaseTypeParser<ulong>
{
}

internal sealed class NIntTypeParser : NumberBaseTypeParser<nint>
{
}

internal sealed class NUIntTypeParser : NumberBaseTypeParser<nuint>
{
}

internal sealed class BigIntegerTypeParser : NumberBaseTypeParser<BigInteger>
{
}

