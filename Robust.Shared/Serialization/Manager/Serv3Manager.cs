using System;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public partial class Serv3Manager : IServ3Manager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public void Initialize()
        {
            InitializeTypeSerializers();
            InitializeDataClasses();
            //todo do actual initialization
        }

        public T ReadValue<T>(IDataNode node, ISerializationContext? context = null)
        {
            throw new System.NotImplementedException();
        }

        public object ReadValue(Type type, IDataNode node, ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public IDataNode WriteValue<T>(T value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            throw new System.NotImplementedException();
        }

        public IDataNode WriteValue(Type type, object value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            throw new NotImplementedException();
        }

        public T Copy<T>(T source, T target)
        {
            throw new System.NotImplementedException();
        }

        public T PushInheritance<T>(T source, T target)
        {
            throw new System.NotImplementedException();
        }
    }
}
