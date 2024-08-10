using System;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using Pidgin;

namespace RobustXaml
{

    public sealed class XamlCustomizations
    {
        public const string ContextNameScopeFieldName = "RobustNameScope";
        public readonly XamlLanguageTypeMappings TypeMappings;
        public readonly XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> EmitMappings;
        public readonly TransformerConfiguration TransformerConfiguration;
        public readonly RobustXamlILCompiler ILCompiler;

        public XamlCustomizations(IXamlTypeSystem typeSystem, IXamlAssembly targetAssembly)
        {
            TypeMappings = new XamlLanguageTypeMappings(typeSystem)
            {
                XmlnsAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute"),

                },
                ContentAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.ContentAttribute")
                },
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.UsableDuringInitializationAttribute")
                },
                DeferredContentPropertyAttributes =
                {
                    typeSystem.GetType("Robust.Client.UserInterface.XAML.DeferredContentAttribute")
                },
                RootObjectProvider = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestRootObjectProvider"),
                UriContextProvider = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestUriContext"),
                ProvideValueTarget = typeSystem.GetType("Robust.Client.UserInterface.XAML.ITestProvideValueTarget"),
            };

            EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
            {
                ContextTypeBuilderCallback = (b, c) =>
                    EmitNameScopeField(TypeMappings, typeSystem, b, c)
            };
            TransformerConfiguration = new TransformerConfiguration(
                typeSystem,
                targetAssembly,
                TypeMappings,
                XamlXmlnsMappings.Resolve(typeSystem, TypeMappings),
                CustomValueConverter
            );
            ILCompiler = new RobustXamlILCompiler(TransformerConfiguration, EmitMappings, true);
        }

        private static void EmitNameScopeField(
            XamlLanguageTypeMappings xamlLanguage,
            IXamlTypeSystem typeSystem,
            IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            IXamlILEmitter constructor
        )
        {
            var nameScopeType = typeSystem.FindType("Robust.Client.UserInterface.XAML.NameScope");
            var field = typeBuilder.DefineField(nameScopeType,
                ContextNameScopeFieldName,
                true,
                false);
            constructor
                .Ldarg_0()
                .Newobj(nameScopeType.GetConstructor())
                .Stfld(field);
        }


        private static bool CustomValueConverter(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IXamlType type,
            out IXamlAstValueNode? result)
        {
            if (!(node is XamlAstTextNode textNode))
            {
                result = null;
                return false;
            }

            var text = textNode.Text;
            var types = context.GetRobustTypes();

            if (type.Equals(types.Vector2))
            {
                var foo = MathParsing.Single2.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Vector2", node);

                var (x, y) = foo.Value;

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Vector2,
                    types.Vector2ConstructorFull,
                    types.Single,
                    new[] { x, y });
                return true;
            }

            if (type.Equals(types.Thickness))
            {
                var foo = MathParsing.Thickness.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Thickness", node);

                var val = foo.Value;
                float[] full;
                if (val.Length == 1)
                {
                    var u = val[0];
                    full = new[] { u, u, u, u };
                }
                else if (val.Length == 2)
                {
                    var h = val[0];
                    var v = val[1];
                    full = new[] { h, v, h, v };
                }
                else // 4
                {
                    full = val;
                }

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Thickness,
                    types.ThicknessConstructorFull,
                    types.Single,
                    full);
                return true;
            }

            if (type.Equals(types.Thickness))
            {
                var foo = MathParsing.Thickness.Parse(text);

                if (!foo.Success)
                    throw new XamlLoadException($"Unable to parse \"{text}\" as a Thickness", node);

                var val = foo.Value;
                float[] full;
                if (val.Length == 1)
                {
                    var u = val[0];
                    full = new[] { u, u, u, u };
                }
                else if (val.Length == 2)
                {
                    var h = val[0];
                    var v = val[1];
                    full = new[] { h, v, h, v };
                }
                else // 4
                {
                    full = val;
                }

                result = new RXamlSingleVecLikeConstAstNode(
                    node,
                    types.Thickness,
                    types.ThicknessConstructorFull,
                    types.Single,
                    full);
                return true;
            }

            if (type.Equals(types.Color))
            {
                // TODO: Interpret these colors at XAML compile time instead of at runtime.
                result = new RXamlColorAstNode(node, types, text);
                return true;
            }

            result = null;
            return false;
        }

        public IFileSource CreateFileSource(string filePath, byte[] contents)
        {
            return new InternalFileSource(filePath, contents);
        }

        class InternalFileSource(string filePath, byte[] contents) : IFileSource
        {
            public string FilePath => filePath;
            public byte[] FileContents => contents;
        }
    }
}
