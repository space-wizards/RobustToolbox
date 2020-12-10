using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Robust.Client.UserInterface.CustomControls;

namespace Robust.Client.UserInterface.XAML
{
    public partial class TestView : SS14Window
    {
        private class DummyService : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }

        public delegate object CallbackExtensionCallback(IServiceProvider provider);
        class DictionaryServiceProvider : Dictionary<Type, object>, IServiceProvider
        {
            public IServiceProvider Parent { get; set; }
            public object GetService(Type serviceType)
            {
                if(TryGetValue(serviceType, out var impl))
                    return impl;
                return Parent?.GetService(serviceType);
            }
        }

        public TestView()
        {
            var comp = new XamlCompiler();
            var content = File.ReadAllText("../../Robust.Client/UserInterface/XAML/TestView.xaml");
            var (create, populate) = comp.Compile(content);

            var testserv = new DictionaryServiceProvider
            {
                [typeof(CallbackExtensionCallback)] = (CallbackExtensionCallback) (cp =>
                {
                    System.Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                    return null;
                }),
                [typeof(DummyService)] = new DummyService()
            };

            create(testserv);
            populate(testserv, this);
            var nameScope = new Dictionary<string, Control>();
            foreach (var control in XamlChildren)
            {
                if(control.Name == null) continue;
                nameScope.Add(control.Name, control);
            }
            AttachNameScope(nameScope);

            //var obj = create(null!);
            //AddChild((Control)obj);
            //throw new NotImplementedException();
            /*

            var thing = XDocumentXamlParser.Parse(content);

            if (thing.Root is XamlAstObjectNode objectNode)
            {
                AddChild(ParseNode(objectNode));
            }

            System.Console.WriteLine("aaa");*/
        }

        /*Control ParseNode(XamlAstObjectNode node)
        {
            foreach (var astNode in node.Children)
            {
                switch (astNode)
                {
                    case XamlAstObjectNode objNode:
                        var type = objNode.Type.GetClrType();
                        break;
                    case XamlAstXamlPropertyValueNode valueNode:
                        break;
                }
            }

            return null;
        }*/
    }



    /*public class TestCompiler
    {
        private readonly IXamlTypeSystem _typeSystem;
        public TransformerConfiguration Configuration { get; }

        public TestCompiler() : this(new SreTypeSystem())
        {

        }

        private TestCompiler(IXamlTypeSystem typeSystem)
        {
            _typeSystem = typeSystem;
            Configuration = new TransformerConfiguration(typeSystem,
                typeSystem.FindAssembly("Robust.Client.UI"),
                new XamlLanguageTypeMappings(typeSystem)
                {
                    XmlnsAttributes =
                    {
                        typeSystem.GetType("Robust.Client.UI.XmlnsDefinitionAttribute"),

                    },
                    ContentAttributes =
                    {
                        typeSystem.GetType("Robust.Client.UI.ContentAttribute")
                    },
                    UsableDuringInitializationAttributes =
                    {
                        typeSystem.GetType("Robust.Client.UI.UsableDuringInitializationAttribute")
                    },
                    DeferredContentPropertyAttributes =
                    {
                        typeSystem.GetType("Robust.Client.UI.DeferredContentAttribute")
                    },
                    RootObjectProvider = typeSystem.GetType("Robust.Client.UI.ITestRootObjectProvider"),
                    UriContextProvider = typeSystem.GetType("Robust.Client.UI.ITestUriContext"),
                    ProvideValueTarget = typeSystem.GetType("Robust.Client.UI.ITestProvideValueTarget"),
                    /*ParentStackProvider = typeSystem.GetType("XamlX.Runtime.IXamlParentStackProviderV1"),
                    XmlNamespaceInfoProvider = typeSystem.GetType("XamlX.Runtime.IXamlXmlNamespaceInfoProviderV1")*/
                /*}
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
    }*/
}
