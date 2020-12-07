using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Robust.Client.UI
{
    public class XamlCompiler : IXamlCompiler
    {
        private readonly IXamlTypeSystem _typeSystem;
        public TransformerConfiguration Configuration { get; }

        public XamlCompiler()
        {
            _typeSystem = new SreTypeSystem();
            Configuration = new TransformerConfiguration(_typeSystem,
                _typeSystem.FindAssembly("Robust.Client.UI"),
                new XamlLanguageTypeMappings(_typeSystem)
                {
                    XmlnsAttributes =
                    {
                        _typeSystem.GetType("Robust.Client.UI.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        _typeSystem.GetType("Robust.Client.UI.ContentAttribute")
                    },
                    UsableDuringInitializationAttributes =
                    {
                        _typeSystem.GetType("Robust.Client.UI.UsableDuringInitializationAttribute")
                    },
                    DeferredContentPropertyAttributes =
                    {
                        _typeSystem.GetType("Robust.Client.UI.DeferredContentAttribute")
                    },
                    RootObjectProvider = _typeSystem.GetType("Robust.Client.UI.ITestRootObjectProvider"),
                    UriContextProvider = _typeSystem.GetType("Robust.Client.UI.ITestUriContext"),
                    ProvideValueTarget = _typeSystem.GetType("Robust.Client.UI.ITestProvideValueTarget"),
                    /*ParentStackProvider = _typeSystem.GetType("XamlX.Runtime.IXamlParentStackProviderV1"),
                    XmlNamespaceInfoProvider = _typeSystem.GetType("XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1")*/
                }
            );
        }

        public (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate) Compile(string xaml)
        {
            var da = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);

            var dm = da.DefineDynamicModule("testasm.dll");
            var t = dm.DefineType(Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            var ct = dm.DefineType(t.Name + "Context");
            var ctb = ((SreTypeSystem)_typeSystem).CreateTypeBuilder(ct);
            var contextTypeDef =
                XamlILContextDefinition.GenerateContextClass(
                    ctb,
                    _typeSystem,
                    Configuration.TypeMappings,
                    new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>());


            var parserTypeBuilder = ((SreTypeSystem) _typeSystem).CreateTypeBuilder(t);

            var parsed = Compile(parserTypeBuilder, contextTypeDef, xaml);

            var created = t.CreateTypeInfo();

            return GetCallbacks(created);
        }

        XamlDocument Compile(IXamlTypeBuilder<IXamlILEmitter> builder, IXamlType context, string xaml)
        {
            var parsed = XDocumentXamlParser.Parse(xaml);
            var compiler = new XamlILCompiler(
                Configuration,
                new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>(),
                true)
            {
                EnableIlVerification = true
            };
            compiler.Transform(parsed);
            compiler.Compile(parsed, builder, context, "Populate", "Build",
                "XamlNamespaceInfo",
                "http://example.com/", null);
            return parsed;
        }

        (Func<IServiceProvider, object> create, Action<IServiceProvider, object> populate)
            GetCallbacks(Type? created)
        {
            if (created == null) throw new NotImplementedException();
            var isp = Expression.Parameter(typeof(IServiceProvider));
            var createCb = Expression.Lambda<Func<IServiceProvider, object>>(
                Expression.Convert(Expression.Call(
                    created.GetMethod("Build")!, isp), typeof(object)), isp).Compile();

            var epar = Expression.Parameter(typeof(object));
            var populate = created.GetMethod("Populate")!;
            isp = Expression.Parameter(typeof(IServiceProvider));
            var populateCb = Expression.Lambda<Action<IServiceProvider, object>>(
                Expression.Call(populate, isp, Expression.Convert(epar, populate.GetParameters()[1].ParameterType)),
                isp, epar).Compile();

            return (createCb, populateCb);
        }
    }
    public class ContentAttribute : Attribute
    {
    }

    public class XmlnsDefinitionAttribute : Attribute
    {
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
        }
    }

    public class UsableDuringInitializationAttribute : Attribute
    {
        public UsableDuringInitializationAttribute(bool usable)
        {
        }
    }

    public class DeferredContentAttribute : Attribute
    {
    }

    public interface ITestRootObjectProvider
    {
        object RootObject { get; }
    }

    public interface ITestProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }

    public interface ITestUriContext
    {
        Uri BaseUri { get; set; }
    }
}
