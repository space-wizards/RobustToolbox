using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Robust.Serialization.Generator
{
    public sealed partial class DataDefinitionGenerator
    {
        public ITypeSymbol ToNonNullableStruct(ITypeSymbol type)
        {
            if (Default.Equals(type.OriginalDefinition, _nullableSymbol))
            {
                type = ((INamedTypeSymbol) type).TypeArguments[0];
            }
        }
    }
}
