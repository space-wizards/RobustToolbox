using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Robust.Shared.Prototypes.DataClasses
{
    public abstract class DataClass : IExposeData
    {
        /// <summary>
        /// gets the mapped value of the given key. null if not mapped. exception if not in datadefinition (no corresponding [YamlField])
        /// </summary>
        public virtual object? GetValue(string tag)
        {
            throw new ArgumentException($"Tag {tag} not defined.", nameof(tag));
        }

        /// <summary>
        /// sets the mapped value of a given key. exception if not in datadefinition (no corresponsing [YamlField])
        /// </summary>
        public virtual void SetValue(string tag, object? value)
        {
            throw new ArgumentException($"Tag {tag} not defined.", nameof(tag));
        }

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            //throw new NotImplementedException();
        }
    }
}
