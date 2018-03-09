using System;

namespace SS14.Server.AI
{
    /// <summary>
    ///     This attribute is used to mark a class as a LogicProcessor for the AI system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AiLogicProcessorAttribute : Attribute
    {
        /// <summary>
        ///     Name of this LogicProcessor in serialized files.
        /// </summary>
        public string SerializeName { get; }

        /// <summary>
        ///     Creates an instance of this Attribute.
        /// </summary>
        /// <param name="serializeName">Name of this LogicProcessor in serialized files.</param>
        public AiLogicProcessorAttribute(string serializeName)
        {
            SerializeName = serializeName;
        }
    }
}
