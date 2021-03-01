using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using static Robust.Shared.Prototypes.EntityPrototype;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedComponentRegistry : DeserializationResult<ComponentRegistry>
    {
        public DeserializedComponentRegistry(
            ComponentRegistry value,
            IReadOnlyDictionary<DeserializationResult, DeserializationResult> mappings)
        {
            Value = value;
            Mappings = mappings;
        }

        public override ComponentRegistry Value { get; }

        public IReadOnlyDictionary<DeserializationResult, DeserializationResult> Mappings { get; }

        public override object? RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            var sourceRes = source.Cast<DeserializedComponentRegistry>();
            var mappingDict = Mappings.ToDictionary(p => p.Key.Copy(), p => p.Value.Copy());

            foreach (var (keyRes, valRes) in sourceRes.Mappings)
            {
                var newKeyRes = keyRes.Copy();
                var newValueRes = valRes.Copy();

                if (mappingDict.Any(p =>
                {
                    var k1 = (string) newKeyRes.RawValue!;
                    var k2 = (string) p.Key.RawValue!;

                    if (k1 == k2)
                    {
                        return false;
                    }

                    var registration1 = componentFactory.GetRegistration(k1!);
                    var registration2 = componentFactory.GetRegistration(k2!);

                    foreach (var reference in registration1.References)
                    {
                        if (registration2.References.Contains(reference))
                        {
                            return true;
                        }
                    }

                    return false;
                }))
                {
                    continue;
                }

                var oldEntry = mappingDict.FirstOrNull(p => Equals(p.Key.RawValue, newKeyRes.RawValue));

                if (oldEntry.HasValue)
                {
                    newKeyRes = oldEntry.Value.Key.PushInheritanceFrom(newKeyRes);
                    newValueRes = oldEntry.Value.Value.PushInheritanceFrom(newValueRes);
                    mappingDict.Remove(oldEntry.Value.Key);
                }

                mappingDict.Add(newKeyRes, newValueRes);
            }

            var valueDict = new ComponentRegistry();
            foreach (var (key, val) in mappingDict)
            {
                valueDict.Add((string) key.RawValue!, (IComponent) val.RawValue!);
            }

            return new DeserializedComponentRegistry(valueDict, mappingDict);
        }

        public override DeserializationResult Copy()
        {
            var registry = new ComponentRegistry();
            var mappingDict = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var (keyRes, valRes) in Mappings)
            {
                var newKeyRes = keyRes.Copy();
                var newValueRes = valRes.Copy();

                registry.Add((string) newKeyRes.RawValue!, (IComponent) newValueRes.RawValue!);
                mappingDict.Add(newKeyRes, newValueRes);
            }

            return new DeserializedComponentRegistry(registry, mappingDict);
        }
    }
}
