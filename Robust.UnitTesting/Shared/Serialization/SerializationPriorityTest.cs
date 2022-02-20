using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using YamlDotNet.RepresentationModel;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization
{
    public sealed class SerializationPriorityTest : RobustUnitTest
    {
        [Test]
        public void Test()
        {
            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            serializationManager.Initialize();

            var prototype = @"
- type: PriorityTest
  first: A
  second: B
  third: C";

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(prototype));

            var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);

            var component = serializationManager.ReadValueOrThrow<PriorityTestComponent>(mapping);

            Assert.That(component.Strings.Count, Is.EqualTo(3));
            Assert.That(component.First, Is.EqualTo("A"));
            Assert.That(component.Second, Is.EqualTo("C"));
            Assert.That(component.Third, Is.EqualTo("B"));
        }
    }

    public sealed class PriorityTestComponent : Component, ISerializationHooks
    {

        public readonly List<string> Strings = new() {string.Empty, string.Empty, string.Empty};

        [DataField("first", priority: 3)]
        public string First
        {
            get => Strings[0];
            set => Strings.Add(value);
        }

        [DataField("second", priority: 1)]
        public string Second
        {
            get => Strings[1];
            set => Strings.Add(value);
        }

        [DataField("third", priority: 2)]
        public string Third
        {
            get => Strings[2];
            set => Strings.Add(value);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            Strings.RemoveRange(0, 3);
        }
    }
}
