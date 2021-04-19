using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private DeserializationResult ReadWithCustomTypeSerializer<T, TNode, TSerializer>(TNode node, ISerializationContext? context = null, bool skipHook = false)
            where TSerializer : ITypeReader<T, TNode> where T : notnull where TNode : DataNode
        {
            var serializer = (ITypeReader<T, TNode>)GetTypeSerializer(typeof(TSerializer));
            return serializer.Read(this, node, DependencyCollection, skipHook, context);
        }

        private DataNode WriteWithCustomTypeSerializer<T, TSerializer>(T value,
            ISerializationContext? context = null, bool alwaysWrite = false)
            where TSerializer : ITypeWriter<T> where T : notnull
        {
            var serializer = (ITypeWriter<T>)GetTypeSerializer(typeof(TSerializer));
            return serializer.Write(this, value, alwaysWrite, context);
        }

        private TCommon CopyWithCustomTypeSerializer<TCommon, TSource, TTarget, TSerializer>(
            TSource source,
            TTarget target,
            bool skipHook,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TCommon : notnull
            where TSerializer : ITypeCopier<TCommon>
        {
            var serializer = (ITypeCopier<TCommon>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Copy(this, source, target, skipHook, context);
        }

        private ValidationNode ValidateWithCustomTypeSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context)
            where T : notnull
            where TNode : DataNode
            where TSerializer : ITypeValidator<T, TNode>
        {
            var serializer = (ITypeValidator<T, TNode>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Validate(this, node, DependencyCollection, context);
        }
    }
}
