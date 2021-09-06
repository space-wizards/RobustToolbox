using System.Diagnostics;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Robust.Build.Tasks
{
    /// <summary>
    /// Emitters & Transformers based on:
    /// - https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Markup/Avalonia.Markup.Xaml.Loader/CompilerExtensions/Transformers/AvaloniaXamlIlRootObjectScopeTransformer.cs
    /// - https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Markup/Avalonia.Markup.Xaml.Loader/CompilerExtensions/Transformers/AddNameScopeRegistration.cs
    /// - https://github.com/AvaloniaUI/Avalonia/blob/afb8ae6f3c517dae912729511483995b16cb31af/src/Markup/Avalonia.Markup.Xaml.Loader/CompilerExtensions/Transformers/IgnoredDirectivesTransformer.cs
    /// </summary>
    public class RobustXamlILCompiler : XamlILCompiler
    {
        public RobustXamlILCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings, bool fillWithDefaults) : base(configuration, emitMappings, fillWithDefaults)
        {
            Transformers.Insert(0, new IgnoredDirectivesTransformer());

            Transformers.Add(new AddNameScopeRegistration());
            Transformers.Add(new RobustMarkRootObjectScopeNode());

            Emitters.Add(new AddNameScopeRegistration.Emitter());
            Emitters.Add(new RobustMarkRootObjectScopeNode.Emitter());
        }

        class AddNameScopeRegistration : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                if (node is XamlPropertyAssignmentNode pa)
                {
                    if (pa.Property.Name == "Name"
                        && pa.Property.DeclaringType.FullName == "Robust.Client.UserInterface.Control")
                    {
                        if (context.ParentNodes().FirstOrDefault() is XamlManipulationGroupNode mg
                            && mg.Children.OfType<RobustNameScopeRegistrationXamlIlNode>().Any())
                            return node;

                        IXamlAstValueNode value = null;
                        for (var c = 0; c < pa.Values.Count; c++)
                            if (pa.Values[c].Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                            {
                                value = pa.Values[c];
                                if (!(value is XamlAstTextNode))
                                {
                                    var local = new XamlAstCompilerLocalNode(value);
                                    // Wrap original in local initialization
                                    pa.Values[c] = new XamlAstLocalInitializationNodeEmitter(value, value, local);
                                    // Use local
                                    value = local;
                                }

                                break;
                            }

                        if (value != null)
                        {
                            var objectType = context.ParentNodes().OfType<XamlAstConstructableObjectNode>().FirstOrDefault()?.Type.GetClrType();
                            return new XamlManipulationGroupNode(pa)
                            {
                                Children =
                                {
                                    pa,
                                    new RobustNameScopeRegistrationXamlIlNode(value, objectType)
                                }
                            };
                        }
                    }
                    /*else if (pa.Property.CustomAttributes.Select(attr => attr.Type).Intersect(context.Configuration.TypeMappings.DeferredContentPropertyAttributes).Any())
                    {
                        pa.Values[pa.Values.Count - 1] =
                            new NestedScopeMetadataNode(pa.Values[pa.Values.Count - 1]);
                    }*/
                }

                return node;
            }

            class RobustNameScopeRegistrationXamlIlNode : XamlAstNode, IXamlAstManipulationNode
            {
                public IXamlAstValueNode Name { get; set; }
                public IXamlType TargetType { get; }

                public RobustNameScopeRegistrationXamlIlNode(IXamlAstValueNode name, IXamlType targetType) : base(name)
                {
                    TargetType = targetType;
                    Name = name;
                }

                public override void VisitChildren(IXamlAstVisitor visitor)
                    => Name = (IXamlAstValueNode)Name.Visit(visitor);
            }

            internal class Emitter : IXamlAstLocalsNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
            {
                public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
                {
                    if (node is RobustNameScopeRegistrationXamlIlNode registration)
                    {

                        var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                            f.Name == XamlCompiler.ContextNameScopeFieldName);
                        var namescopeRegisterFunction = context.Configuration.TypeSystem
                            .FindType("Robust.Client.UserInterface.XAML.NameScope").Methods
                            .First(m => m.Name == "Register");

                        using (var targetLoc = context.GetLocalOfType(context.Configuration.TypeSystem.FindType("Robust.Client.UserInterface.Control")))
                        {

                            codeGen
                                // var target = {pop}
                                .Stloc(targetLoc.Local)
                                // _context.NameScope.Register(Name, target)
                                .Ldloc(context.ContextLocal)
                                .Ldfld(scopeField);

                            context.Emit(registration.Name, codeGen, registration.Name.Type.GetClrType());

                            codeGen
                                .Ldloc(targetLoc.Local)
                                .EmitCall(namescopeRegisterFunction, true);
                        }

                        return XamlILNodeEmitResult.Void(1);
                    }
                    return default;
                }
            }
        }

        class RobustMarkRootObjectScopeNode : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                if (!context.ParentNodes().Any()
                    && node is XamlValueWithManipulationNode mnode)
                {
                    mnode.Manipulation = new XamlManipulationGroupNode(mnode,
                        new[]
                        {
                            mnode.Manipulation,
                            new HandleRootObjectScopeNode(mnode)
                        });
                }
                return node;
            }
            class HandleRootObjectScopeNode : XamlAstNode, IXamlAstManipulationNode
            {
                public HandleRootObjectScopeNode(IXamlLineInfo lineInfo) : base(lineInfo)
                {
                }
            }
            internal class Emitter : IXamlILAstNodeEmitter
            {
                public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
                {
                    if (!(node is HandleRootObjectScopeNode))
                    {
                        return null;
                    }

                    var controlType = context.Configuration.TypeSystem.FindType("Robust.Client.UserInterface.Control");

                    var next = codeGen.DefineLabel();
                    var dontAbsorb = codeGen.DefineLabel();
                    var end = codeGen.DefineLabel();
                    var contextScopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                        f.Name == XamlCompiler.ContextNameScopeFieldName);
                    var controlNameScopeField = controlType.Fields.First(f => f.Name == "NameScope");
                    var nameScopeType = context.Configuration.TypeSystem
                        .FindType("Robust.Client.UserInterface.XAML.NameScope");
                    var nameScopeCompleteMethod = nameScopeType.Methods.First(m => m.Name == "Complete");
                    var nameScopeAbsorbMethod = nameScopeType.Methods.First(m => m.Name == "Absorb");
                    using (var local = codeGen.LocalsPool.GetLocal(controlType))
                    {
                        codeGen
                            .Isinst(controlType)
                            .Dup()
                            .Stloc(local.Local) //store control in local field
                            .Brfalse(next) //if control is null, move to next (this should never happen but whatev, avalonia does it)
                            .Ldloc(context.ContextLocal)
                            .Ldfld(contextScopeField)
                            .Ldloc(local.Local) //load control from local field
                            .Ldfld(controlNameScopeField) //load namescope field from control
                            .EmitCall(nameScopeAbsorbMethod, true)
                            .Ldloc(local.Local) //load control
                            .Ldloc(context.ContextLocal) //load contextObject
                            .Ldfld(contextScopeField) //load namescope field from context obj
                            .Stfld(controlNameScopeField) //store namescope field in control
                            .MarkLabel(next)
                            .Ldloc(context.ContextLocal)
                            .Ldfld(contextScopeField)
                            .EmitCall(nameScopeCompleteMethod, true); //set the namescope as complete
                    }

                    return XamlILNodeEmitResult.Void(1);
                }
            }
        }

        class IgnoredDirectivesTransformer : IXamlAstTransformer
        {
            public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
            {
                if (node is XamlAstObjectNode astNode)
                {
                    astNode.Children.RemoveAll(n =>
                        n is XamlAstXmlDirective dir &&
                        dir.Namespace == XamlNamespaces.Xaml2006 &&
                        (dir.Name == "Class" ||
                         dir.Name == "Precompile" ||
                         dir.Name == "FieldModifier" ||
                         dir.Name == "ClassModifier"));
                }

                return node;
            }
        }
    }
}
