using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

[TypeSerializer]
public sealed class AlertOrderSerializer : ITypeReader<SortedDictionary<(string?,string?),int>, SequenceDataNode>
{
    public DeserializationResult Read(ISerializationManager serializationManager, SequenceDataNode alertOrder,
        IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
    {
        var deserializedAlertOrder = new SortedDictionary<(string?, string?),int>();
        int idx = 0;
        foreach (var alertOrderEntry in alertOrder.Sequence)
        {
            if (alertOrderEntry is not ValueDataNode key)
            {
                Logger.Error("failed to parse alertOrder entry key as it is not a MappingDataNode");
                continue;
            }
            switch (key.Tag)
            {
                case "category":
                    deserializedAlertOrder[(key.Value, null)] = idx;
                    idx++;
                    break;
                case "alertType:":
                    deserializedAlertOrder[("", key.Value)] = idx;
                    idx++;
                    break;
            }
        }
        return new DeserializedValue<SortedDictionary<(string?, string?),int>>(deserializedAlertOrder);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode alertOrder,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        foreach (var alertOrderEntry in alertOrder.Sequence)
        {
            if (alertOrderEntry is ValueDataNode key) //TODO check Entry Value to see if the alertType is loaded.
            {
                if (key.Value is "category" or "alertType")
                {
                    continue;
                }
                return new ErrorNode(alertOrder, "failed to parse alertOrder, key was not recognized as category or alertType");
            }
            return new ErrorNode(alertOrder, "failed to parse alertOrder, key/value is not a ValueDataNode");
        }
        return new ValidatedValueNode(alertOrder);
    }

}
