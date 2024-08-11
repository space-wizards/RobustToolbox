using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using Pidgin;

namespace RobustXaml
{

    /// <summary>
    /// This is a bunch of code primarily written by PJB that originally appeared in XamlAotCompiler.cs.
    ///
    /// It now appears here because it's useful to both the AOT and the JIT compiler.
    /// </summary>
    public sealed class XamlCustomizations
    {
        public const string ContextNameScopeFieldName = "RobustNameScope";
        public readonly IXamlTypeSystem TypeSystem;
        public readonly XamlLanguageTypeMappings TypeMappings;
        public readonly XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> EmitMappings;
        public readonly TransformerConfiguration TransformerConfiguration;
        public readonly RobustXamlILCompiler ILCompiler;

        /// <summary>
        /// Create a bunch of resources related to SS14's particular dialect of XAML.
        /// </summary>
        /// <param name="typeSystem">the type system for XamlX to use (both Cecil and Sre work)</param>
        /// <param name="defaultAssembly">the default assembly (presumably for unqualified names to be looked up in)</param>
        public XamlCustomizations(IXamlTypeSystem typeSystem, IXamlAssembly defaultAssembly)
        {
            TypeSystem = typeSystem;
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
                ContextTypeBuilderCallback = EmitNameScopeField
            };
            TransformerConfiguration = new TransformerConfiguration(
                typeSystem,
                defaultAssembly,
                TypeMappings,
                XamlXmlnsMappings.Resolve(typeSystem, TypeMappings),
                CustomValueConverter
            );
            ILCompiler = new RobustXamlILCompiler(TransformerConfiguration, EmitMappings, true);
        }

        /// <summary>
        /// Create a field of type NameScope that contains a new NameScope.
        ///
        /// Alter the type's constructor to initialize that field.
        /// </summary>
        /// <param name="typeBuilder">the type to alter</param>
        /// <param name="constructor">the constructor to alter</param>
        private void EmitNameScopeField(
            IXamlTypeBuilder<IXamlILEmitter> typeBuilder,
            IXamlILEmitter constructor
        )
        {
            var nameScopeType = TypeSystem.FindType("Robust.Client.UserInterface.XAML.NameScope");
            var field = typeBuilder.DefineField(nameScopeType,
                ContextNameScopeFieldName,
                true,
                false);
            constructor
                .Ldarg_0()
                .Newobj(nameScopeType.GetConstructor())
                .Stfld(field);
        }


        /// <summary>
        /// Convert a XamlAstTextNode to some other kind of node, if the purpose of the node appears to be
        /// to represent one of various builtin types. (See, for instance, RXamlColorAstNode.)
        ///
        /// The arguments here come from an interface built into XamlX.
        /// </summary>
        /// <param name="context">context object that holds the TransformerConfiguration</param>
        /// <param name="node">the node to consider rewriting</param>
        /// <param name="type">the type of that node</param>
        /// <param name="result">results get written to here</param>
        /// <returns></returns>
        /// <exception cref="XamlLoadException">if the literal for a type is poorly spelled for that type</exception>
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

        /// <summary>
        /// Wrap the filePath and contents from a XamlMetadataAttribute.
        ///
        /// This is handy for feeding the data to a XamlX compiler.
        /// </summary>
        /// <param name="filePath">the resource file path</param>
        /// <param name="contents">the contents</param>
        /// <returns></returns>
        public IFileSource CreateFileSource(string filePath, byte[] contents)
        {
            return new InternalFileSource(filePath, contents);
        }

        /// <summary>
        /// A trivial implementation of IFileSource.
        /// </summary>
        /// <param name="filePath">the path</param>
        /// <param name="contents">the contents</param>
        class InternalFileSource(string filePath, byte[] contents) : IFileSource
        {
            public string FilePath => filePath;
            public byte[] FileContents => contents;
        }
    }
}
