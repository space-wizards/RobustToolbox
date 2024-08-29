﻿using System.Linq;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization
{
    public sealed partial class PropertyAndFieldDefinitionTest : SerializationTest
    {
        private const string GetOnlyPropertyName = "GetOnlyProperty";
        private const string GetOnlyPropertyFieldTargetedName = "GetOnlyPropertyFieldTargeted";
        private const string GetAndSetPropertyName = "GetAndSetProperty";
        private const string FieldName = "Field";
        private const string GetOnlyPropertyWithOtherAttributeFieldTargetedName =
            "GetOnlyPropertyWithOtherAttributeFieldTargeted";
        private const string GetOnlyPropertyFieldTargetedAndOtherAttributeName =
            "GetOnlyPropertyFieldTargetedAndOtherAttribute";

        [Test]
        public void ParityTest()
        {
            var mapping = new MappingDataNode();
            mapping.Add(GetOnlyPropertyName, new ValueDataNode("5"));
            mapping.Add(GetOnlyPropertyFieldTargetedName, new ValueDataNode("10"));
            mapping.Add(GetAndSetPropertyName, new ValueDataNode("15"));
            mapping.Add(FieldName, new ValueDataNode("20"));
            mapping.Add(GetOnlyPropertyWithOtherAttributeFieldTargetedName, new ValueDataNode("25"));
            mapping.Add(GetOnlyPropertyFieldTargetedAndOtherAttributeName, new ValueDataNode("30"));

            var definition = Serialization.Read<PropertyAndFieldDefinitionTestDefinition>(mapping, notNullableOverride: true);

            Assert.That(definition, Is.Not.Null);

            // Get only property with backing field, property targeted
            Assert.That(definition!.GetOnlyProperty, Is.EqualTo(5));

            var backingField = definition.GetType().GetBackingField(GetOnlyPropertyName);
            Assert.That(backingField, Is.Not.Null);

            var backingFieldValue = backingField!.GetValue(definition);
            Assert.That(backingFieldValue, Is.EqualTo(5));

            // Get only property with backing field, field targeted
            Assert.That(definition.GetOnlyPropertyFieldTargeted, Is.EqualTo(10));

            // Get and set property with backing field, property targeted
            Assert.That(definition.GetAndSetProperty, Is.EqualTo(15));

            // Field
            Assert.That(definition.Field, Is.EqualTo(20));

            // Get only property with backing field, property targeted with another attribute field targeted
            Assert.That(definition.GetOnlyPropertyWithOtherAttributeFieldTargeted, Is.EqualTo(25));

            var property = definition.GetType().GetProperty(GetOnlyPropertyWithOtherAttributeFieldTargetedName);
            Assert.That(property, Is.Not.Null);

            var propertyInfo = new SpecificPropertyInfo(property!);
            Assert.That(propertyInfo.GetAttribute<DataFieldAttribute>(), Is.Not.Null);
            Assert.That(propertyInfo.GetAttribute<AlwaysPushInheritanceAttribute>(), Is.Not.Null);

            // We check for the property info properly finding field targeted attributes as
            // well, otherwise we run the risk of the data field being targeted to the
            // property but an additional attribute like AlwaysPushInheritance being targeted
            // to the field, as was the case in EntityPrototype.
            // And I don't want to debug that ever again.
            Assert.That(propertyInfo.DeclaringType, Is.Not.Null);

            var dataDefinition = ((SerializationManager) Serialization).GetDefinition(propertyInfo.DeclaringType!);
            Assert.That(dataDefinition, Is.Not.Null);

            var alwaysPushDataField = propertyInfo.GetAttribute<DataFieldAttribute>();
            var propertyDefinition =
                dataDefinition!.BaseFieldDefinitions.Single(e => e.Attribute.Equals(alwaysPushDataField));
            var inheritanceBehaviour = propertyDefinition.InheritanceBehavior;
            Assert.That(inheritanceBehaviour, Is.EqualTo(InheritanceBehavior.Always));

            // Get only property with backing field, field targeted with another attribute property targeted
            Assert.That(definition.GetOnlyPropertyFieldTargetedAndOtherAttribute, Is.EqualTo(30));

            property = definition.GetType().GetProperty(GetOnlyPropertyFieldTargetedAndOtherAttributeName);
            Assert.That(property, Is.Not.Null);

            propertyInfo = new SpecificPropertyInfo(property!);

            // Data field is targeted to the backing field
            Assert.That(propertyInfo.GetAttribute<DataFieldAttribute>(), Is.Not.Null);
            Assert.That(propertyInfo.GetBackingField()!.GetAttribute<DataFieldAttribute>(), Is.Null);
            Assert.That(propertyInfo.GetAttribute<DataFieldAttribute>(true), Is.Not.Null);

            // NeverPushInheritanceAttribute is targeted to the property
            Assert.That(propertyInfo.GetAttribute<NeverPushInheritanceAttribute>(), Is.Not.Null);
            Assert.That(propertyInfo.GetBackingField()!.GetAttribute<NeverPushInheritanceAttribute>(), Is.Null);
            Assert.That(propertyInfo.GetAttribute<NeverPushInheritanceAttribute>(true), Is.Not.Null);

            var neverPushDataField = propertyInfo.GetAttribute<DataFieldAttribute>();
            propertyDefinition =
                dataDefinition!.BaseFieldDefinitions.Single(e => e.Attribute.Equals(neverPushDataField));
            inheritanceBehaviour = propertyDefinition.InheritanceBehavior;
            dataDefinition = ((SerializationManager) Serialization).GetDefinition(property!.DeclaringType!);
            Assert.That(dataDefinition, Is.Not.Null);
            Assert.That(inheritanceBehaviour, Is.EqualTo(InheritanceBehavior.Never));
        }

        [Robust.Shared.Serialization.Manager.Attributes.DataDefinition]
        public sealed partial class PropertyAndFieldDefinitionTestDefinition
        {
            [DataField(GetOnlyPropertyName)]
            public int GetOnlyProperty { get; private set; }

            [DataField(GetOnlyPropertyFieldTargetedName)]
            public int GetOnlyPropertyFieldTargeted { get; private set; }

            [DataField(GetAndSetPropertyName)]
            public int GetAndSetProperty { get; set; }

            [DataField(FieldName)]
            // ReSharper disable once UnassignedField.Global
            public int Field;

            [DataField(GetOnlyPropertyWithOtherAttributeFieldTargetedName)]
            [AlwaysPushInheritance]
            public int GetOnlyPropertyWithOtherAttributeFieldTargeted { get; private set; }

            [DataField(GetOnlyPropertyFieldTargetedAndOtherAttributeName)]
            [NeverPushInheritance]
            public int GetOnlyPropertyFieldTargetedAndOtherAttribute { get; private set; }
        }
    }
}
