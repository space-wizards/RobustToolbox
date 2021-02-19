using System.Linq;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Robust.Build.Tasks
{
    class RXamlWellKnownTypes
    {
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlType Single { get; }
        public IXamlType Int32 { get; }
        public IXamlType Vector2 { get; }
        public IXamlConstructor Vector2ConstructorFull { get; }
        public IXamlType Vector2i { get; }
        public IXamlConstructor Vector2iConstructorFull { get; }
        public IXamlType Thickness { get; }
        public IXamlConstructor ThicknessConstructorFull { get; }
        public RXamlWellKnownTypes(TransformerConfiguration cfg)
        {
            var ts = cfg.TypeSystem;
            XamlIlTypes = cfg.WellKnownTypes;
            Single = ts.GetType("System.Single");
            Int32 = ts.GetType("System.Int32");

            (Vector2, Vector2ConstructorFull) = GetNumericTypeInfo("Robust.Shared.Maths.Vector2", Single, 2);
            (Vector2i, Vector2iConstructorFull) = GetNumericTypeInfo("Robust.Shared.Maths.Vector2i", Int32, 2);
            (Thickness, ThicknessConstructorFull) = GetNumericTypeInfo("Robust.Shared.Maths.Thickness", Single, 4);

            (IXamlType, IXamlConstructor) GetNumericTypeInfo(string name, IXamlType componentType, int componentCount)
            {
                var type = cfg.TypeSystem.GetType(name);
                var ctor = type.GetConstructor(Enumerable.Repeat(componentType, componentCount).ToList());

                return (type, ctor);
            }
        }
    }

    static class RXamlWellKnownTypesExtensions
    {
        public static RXamlWellKnownTypes GetRobustTypes(this AstTransformationContext ctx)
        {
            if (ctx.TryGetItem<RXamlWellKnownTypes>(out var rv))
                return rv;
            ctx.SetItem(rv = new RXamlWellKnownTypes(ctx.Configuration));
            return rv;
        }
    }
}
