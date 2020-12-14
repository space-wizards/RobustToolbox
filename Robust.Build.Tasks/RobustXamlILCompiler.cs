using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;

namespace Robust.Build.Tasks
{
    public class RobustXamlILCompiler : XamlX.IL.XamlILCompiler
    {
        public RobustXamlILCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings, bool fillWithDefaults) : base(configuration, emitMappings, fillWithDefaults)
        {
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
                var contextScopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                    f.Name == XamlCompiler.ContextNameScopeFieldName);
                var controlScopeField = controlType.Fields.First(f => f.Name == "_nameScope");
                var nameScopeCompleteMethod = context.Configuration.TypeSystem
                    .FindType("Robust.Client.UserInterface.XAML.NameScope").Methods.First(m => m.Name == "Complete");
                using (var local = codeGen.LocalsPool.GetLocal(controlType))
                {
                    codeGen
                        .Isinst(controlType)
                        .Dup()
                        .Stloc(local.Local)
                        .Brfalse(next)
                        .Ldloc(local.Local)
                        .Ldloc(context.ContextLocal)
                        .Ldfld(contextScopeField)
                        .Stfld(controlScopeField)
                        //.EmitCall(types.NameScopeSetNameScope, true)
                        .MarkLabel(next)
                        .Ldloc(context.ContextLocal)
                        .Ldfld(contextScopeField)
                        .EmitCall(nameScopeCompleteMethod, true);
                }

                return XamlILNodeEmitResult.Void(1);
            }
        }
    }
    }
}
