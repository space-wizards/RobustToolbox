using System.Linq;
using Microsoft.CodeAnalysis;

namespace Robust.Serialization.Generator
{
    public sealed partial class DataDefinitionGenerator
    {
        private bool TryGetRecordCopier(INamedTypeSymbol type, string copyVariableName, out string copier)
        {
            if (!type.IsRecord)
            {
                copier = null;
                return false;
            }

            var fields = type.GetMembers()
                .Where(m =>
                    m.Kind == SymbolKind.Field ||
                    m.Kind == SymbolKind.Property)
                .Select(m => m.Name)
                .ToArray();
            var constructor = type.InstanceConstructors
                .FirstOrDefault(c =>
                    c.Parameters
                        .Where(p => !p.IsImplicitlyDeclared)
                        .Any(p => fields.Contains(p.Name)) &&
                    !c.IsImplicitlyDeclared);

            if (constructor == null)
            {
                copier = null;
                return false;
            }

            var parameters = constructor.Parameters.Select(p => $"{copyVariableName}.{p.Name}");
            var constructorArguments = string.Join(",", parameters);
            copier = $"new({constructorArguments});";
            return true;
        }
    }
}
