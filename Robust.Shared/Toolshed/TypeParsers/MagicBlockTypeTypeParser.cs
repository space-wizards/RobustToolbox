using System;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class MagicBlockTypeTypeParser
{

}

internal readonly record struct MagicBlockType(Type T) : IAsType<Type>
{
    public Type AsType()
    {
        return T;
    }
}
