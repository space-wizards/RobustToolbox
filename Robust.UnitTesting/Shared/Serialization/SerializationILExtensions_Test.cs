using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Serialization
{
    public class SerializationILExtensions_Test : RobustUnitTest
    {
        private const string YAMLNESTED = @"
intField: 4
nested:
  - strField: bar
";

        private const string YAML = @"
strField: foo
";

        [DataDefinition]
        private class TestClass
        {
            [DataField("intField")]
            public int primitiveField = 2;
            [DataField("nested")]
            public NestedTestClass nestedField = new(){primitiveStringField = "defaultTest"};
        }

        [DataDefinition]
        private class NestedTestClass
        {
            [DataField("strField")]
            public string primitiveStringField { get; set; } = "defaultTest2";
        }

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        /* TODO PAUL
        [Test]
        public void PopulateFieldTest()
        {
            var dynMethod = new DynamicMethod(
                $"_testMethodPopulate",
                typeof(void),
                new[] {typeof(object), typeof(YamlObjectSerializer), typeof(ISerializationManager), typeof(object?[])},
                typeof(NestedTestClass),
                true);
            dynMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            var generator = dynMethod.GetILGenerator();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            var dataDef = serializationManager.GetDataDefinition(typeof(TestClass));
            Assert.NotNull(dataDef);
            Assert.That(dataDef!.FieldDefinitions.Count, Is.EqualTo(2));

            var field = dataDef.FieldDefinitions.First(f => f.Attribute.Tag == "nested");
            var localfield = generator.DeclareLocal(field.FieldType);

            generator.EmitPopulateField(field, localfield.LocalIndex, 0);

            generator.Emit(OpCodes.Ret);

            var @delegate = dynMethod.CreateDelegate<Action<object, YamlObjectSerializer, ISerializationManager, object?[]>>();

            var test = new TestClass();
            var mapping = YamlObjectSerializer_Test.YamlTextToNode(YAMLNESTED);
            @delegate(test, YamlObjectSerializer.NewReader(mapping), serializationManager, new [] {field.DefaultValue});

            Assert.That(test.nestedField.primitiveStringField, Is.EqualTo("bar"));
        }

        [Test]
        public void PopulateFieldPrimitiveOnlyTest()
        {
            var dynMethod = new DynamicMethod(
                $"_testMethodPopulate",
                typeof(void),
                new[] {typeof(object), typeof(YamlObjectSerializer), typeof(ISerializationManager), typeof(object?[])},
                typeof(NestedTestClass),
                true);
            dynMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            var generator = dynMethod.GetILGenerator();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            var dataDef = serializationManager.GetDataDefinition(typeof(NestedTestClass));
            Assert.NotNull(dataDef);
            Assert.That(dataDef!.FieldDefinitions.Count, Is.EqualTo(1));

            var field = dataDef.FieldDefinitions.First();
            var localfield = generator.DeclareLocal(field.FieldType);

            generator.EmitPopulateField(field, localfield.LocalIndex, 0);

            generator.Emit(OpCodes.Ret);

            var @delegate = dynMethod.CreateDelegate<Action<object, YamlObjectSerializer, ISerializationManager, object?[]>>();

            var test = new NestedTestClass();
            var mapping = YamlObjectSerializer_Test.YamlTextToNode(YAML);
            @delegate(test, YamlObjectSerializer.NewReader(mapping), serializationManager, new [] {field.DefaultValue});

            Assert.That(test.primitiveStringField, Is.EqualTo("foo"));
        }

        [Test]
        public void PopulateFieldDefaultPrimitiveOnlyTest()
        {
            var dynMethod = new DynamicMethod(
                $"_testMethodPopulate",
                typeof(void),
                new[] {typeof(object), typeof(YamlObjectSerializer), typeof(ISerializationManager), typeof(object?[])},
                typeof(NestedTestClass),
                true);
            dynMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            var generator = dynMethod.GetILGenerator();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            var dataDef = serializationManager.GetDataDefinition(typeof(NestedTestClass));
            Assert.NotNull(dataDef);
            Assert.That(dataDef!.FieldDefinitions.Count, Is.EqualTo(1));

            var field = dataDef.FieldDefinitions.First();
            var localfield = generator.DeclareLocal(field.FieldType);

            generator.EmitPopulateField(field, localfield.LocalIndex, 0);

            generator.Emit(OpCodes.Ret);

            var @delegate = dynMethod.CreateDelegate<Action<object, YamlObjectSerializer, ISerializationManager, object?[]>>();

            var test = new NestedTestClass();
            var mapping = new YamlMappingNode();
            @delegate(test, YamlObjectSerializer.NewReader(mapping), serializationManager, new []{field.DefaultValue});

            Assert.That(test.primitiveStringField, Is.EqualTo("defaultTest2"));
        }

        [Test]
        public void CopyPrimitiveTest()
        {
            var dynMethod = new DynamicMethod(
                $"_testMethodPopulate",
                typeof(void),
                new[] {typeof(object), typeof(object), typeof(ISerializationManager)},
                typeof(NestedTestClass),
                true);
            dynMethod.DefineParameter(1, ParameterAttributes.In, "obj1");
            dynMethod.DefineParameter(2, ParameterAttributes.In, "obj2");
            dynMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            var generator = dynMethod.GetILGenerator();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            var dataDef = serializationManager.GetDataDefinition(typeof(NestedTestClass));
            Assert.NotNull(dataDef);
            Assert.That(dataDef!.FieldDefinitions.Count, Is.EqualTo(1));

            var field = dataDef.FieldDefinitions.First();

            generator.EmitCopy(0, field.FieldInfo, 1, field.FieldInfo, 2);

            generator.Emit(OpCodes.Ret);

            var @delegate = dynMethod.CreateDelegate<Action<object, Object, ISerializationManager>>();

            var source = new NestedTestClass(){primitiveStringField = "copyTest"};
            var target = new NestedTestClass();
            @delegate(source, target, serializationManager);

            Assert.That(target.primitiveStringField, Is.EqualTo("copyTest"));
        }*/
    }
}
