using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Manager.Result
{
    public class DeserializedArray : DeserializationResult
    {
        public DeserializedArray(Array array, IEnumerable<DeserializationResult> mappings)
        {
            Value = array;
            Mappings = mappings;
        }

        public Array Value { get; }

        public IEnumerable<DeserializationResult> Mappings { get; }

        public override object RawValue => Value;

        public override DeserializationResult PushInheritanceFrom(DeserializationResult source)
        {
            var sourceCollection = source.Cast<DeserializedArray>();
            var valueList = (Array) Activator.CreateInstance(Value.GetType(), Value.Length)!;
            var resList = new List<DeserializationResult>();

            var i = 0;
            foreach (var oldRes in sourceCollection.Mappings)
            {
                var newRes = oldRes.Copy();
                valueList.SetValue(newRes.RawValue, i);
                resList.Add(newRes);
                i++;
            }

            i = 0;
            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy();
                valueList.SetValue(newRes.RawValue, i);
                resList.Add(newRes);
                i++;
            }

            return new DeserializedArray(valueList, resList);
        }

        public override DeserializationResult Copy()
        {
            var valueList = (Array) Activator.CreateInstance(Value.GetType(), Value.Length)!;
            var resList = new List<DeserializationResult>();

            var i = 0;
            foreach (var oldRes in Mappings)
            {
                var newRes = oldRes.Copy();
                valueList.SetValue(newRes.RawValue, i);
                resList.Add(newRes);
                i++;
            }

            return new DeserializedArray(valueList, resList);
        }

        public override void CallAfterDeserializationHook()
        {
            foreach (var elem in Mappings)
            {
                elem.CallAfterDeserializationHook();
            }
        }
    }
}
