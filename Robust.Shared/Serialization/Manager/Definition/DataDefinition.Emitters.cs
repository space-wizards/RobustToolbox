using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition<T>
    {
        private PopulateDelegateSignature EmitPopulateDelegate(SerializationManager manager)
        {
            var isServer = manager.DependencyCollection.Resolve<INetManager>().IsServer;

            var managerConst = Expression.Constant(manager);

            var targetParam = Expression.Parameter(typeof(T).MakeByRefType());
            var mappingDataParam = Expression.Parameter(typeof(MappingDataNode));
            var contextParam = Expression.Parameter(typeof(ISerializationContext));
            var skipHookParam = Expression.Parameter(typeof(bool));

            var expressions = new List<BlockExpression>();

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                if (fieldDefinition.Attribute.ServerOnly && !isServer)
                {
                    continue;
                }

                var nodeVariable = Expression.Variable(typeof(DataNode));
                var valueVariable = Expression.Variable(fieldDefinition.FieldType);
                Expression call;
                if (fieldDefinition.Attribute.CustomTypeSerializer != null && (FieldInterfaceInfos[i].Reader.Value ||
                                                                               FieldInterfaceInfos[i].Reader.Sequence ||
                                                                               FieldInterfaceInfos[i].Reader.Mapping))
                {
                    var switchCases = new List<SwitchCase>();

                    var dependencyConst = Expression.Constant(manager.DependencyCollection);
                    var serializerInstance =
                        manager.GetOrCreateCustomTypeSerializer(fieldDefinition.Attribute.CustomTypeSerializer);
                    if (FieldInterfaceInfos[i].Reader.Value)
                    {
                        var serializerType =
                            typeof(ITypeReader<,>).MakeGenericType(fieldDefinition.FieldType, typeof(ValueDataNode));
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void), Expression.Assign(valueVariable, Expression.Call(
                            Expression.Constant(serializerInstance, serializerType),
                            serializerType.GetMethod("Read")!,
                            managerConst,
                            Expression.Convert(nodeVariable, typeof(ValueDataNode)),
                            dependencyConst,
                            skipHookParam,
                            contextParam,
                            Expression.Default(fieldDefinition.FieldType)))),
                            Expression.Constant(typeof(ValueDataNode))));
                    }

                    if (FieldInterfaceInfos[i].Reader.Sequence)
                    {
                        var serializerType =
                            typeof(ITypeReader<,>).MakeGenericType(fieldDefinition.FieldType, typeof(SequenceDataNode));
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void), Expression.Assign(valueVariable, Expression.Call(
                            Expression.Constant(serializerInstance, serializerType),
                            serializerType.GetMethod("Read")!,
                            managerConst,
                            Expression.Convert(nodeVariable, typeof(SequenceDataNode)),
                            dependencyConst,
                            skipHookParam,
                            contextParam,
                            Expression.Default(fieldDefinition.FieldType)))),
                            Expression.Constant(typeof(SequenceDataNode))));
                    }

                    if (FieldInterfaceInfos[i].Reader.Mapping)
                    {

                        var serializerType =
                            typeof(ITypeReader<,>).MakeGenericType(fieldDefinition.FieldType, typeof(MappingDataNode));
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void), Expression.Assign(valueVariable, Expression.Call(
                                Expression.Constant(serializerInstance, serializerType),
                                serializerType.GetMethod("Read")!,
                                managerConst,
                                Expression.Convert(nodeVariable, typeof(MappingDataNode)),
                                dependencyConst,
                                skipHookParam,
                                contextParam,
                                Expression.Default(fieldDefinition.FieldType)))),
                            Expression.Constant(typeof(MappingDataNode))));
                    }

                    call = Expression.Switch(Expression.Call(nodeVariable, "GetType", Type.EmptyTypes),
                        SerializationManager.ThrowExpression<InvalidOperationException>(),
                        switchCases.ToArray());
                }
                else
                {
                    call = Expression.Assign(valueVariable, Expression.Call(
                        managerConst,
                        "Read",
                        new[] { fieldDefinition.FieldType },
                        nodeVariable,
                        contextParam,
                        skipHookParam,
                        Expression.Default(fieldDefinition.FieldType)));
                }

                call = Expression.Block(
                    new[] { valueVariable },
                    call,
                    Expression.IfThen(
                        Expression.Not(IsDefault(i, valueVariable, fieldDefinition)),
                        Expression.Invoke(Expression.Constant(FieldAssigners[i]), targetParam,
                            Expression.Convert(valueVariable, typeof(object)))));


                if (fieldDefinition.Attribute is DataFieldAttribute dfa)
                {
                    var tagConst = Expression.Constant(dfa.Tag);

                    expressions.Add(Expression.Block(
                        new []{nodeVariable},
                        Expression.IfThenElse(
                        Expression.Call(
                            mappingDataParam,
                            typeof(MappingDataNode).GetMethod("TryGet",
                                new [] { typeof(string), typeof(DataNode).MakeByRefType() })!,
                            tagConst,
                            nodeVariable),
                        call,
                        dfa.Required
                            ? SerializationManager.ThrowExpression<RequiredFieldNotMappedException>(fieldDefinition.FieldType, tagConst)
                            : Expression.Empty()
                    )));
                }
                else
                {
                    expressions.Add(Expression.Block(
                        new []{nodeVariable},
                        Expression.Assign(nodeVariable, mappingDataParam),
                        call));
                }
            }

            return Expression.Lambda<PopulateDelegateSignature>(
                Expression.Block(expressions),
                targetParam,
                mappingDataParam,
                contextParam,
                skipHookParam).Compile();
        }

        private SerializeDelegateSignature EmitSerializeDelegate(SerializationManager manager)
        {
            var managerConst = Expression.Constant(manager);
            var isServer = manager.DependencyCollection.Resolve<INetManager>().IsServer;

            var objParam = Expression.Parameter(typeof(T));
            var contextParam = Expression.Parameter(typeof(ISerializationContext));
            var alwaysWriteParam = Expression.Parameter(typeof(bool));

            var expressions = new List<Expression>();
            var mappingDataVar = Expression.Variable(typeof(MappingDataNode));

            expressions.Add(
                Expression.Assign(
                    mappingDataVar,
                    SerializationManager.NewExpression<MappingDataNode>()
                ));

            for (var i = BaseFieldDefinitions.Length - 1; i >= 0; i--)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                if (fieldDefinition.Attribute.ReadOnly)
                {
                    continue;
                }

                if (fieldDefinition.Attribute.ServerOnly && !isServer)
                {
                    continue;
                }

                Expression call;
                var valueVar = Expression.Variable(fieldDefinition.FieldType);
                if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Writer)
                {
                    var serializerInstance =
                        manager.GetOrCreateCustomTypeSerializer(fieldDefinition.Attribute.CustomTypeSerializer);
                    var dependencyConst = Expression.Constant(manager.DependencyCollection);
                    var serializerType =
                        typeof(ITypeWriter<>).MakeGenericType(fieldDefinition.FieldType);
                    call = Expression.Call(
                        Expression.Constant(serializerInstance, serializerType),
                        serializerType.GetMethod("Write")!,
                        managerConst,
                        valueVar,
                        dependencyConst,
                        alwaysWriteParam,
                        contextParam);
                }
                else
                {
                    call = Expression.Call(
                        managerConst,
                        "WriteValue",
                        new[] { fieldDefinition.FieldType },
                        valueVar,
                        alwaysWriteParam,
                        contextParam);
                }

                call = ExpressionUtils.WriteLineBefore($"calling {i}", call);

                Expression writeExpression;
                var nodeVariable = Expression.Variable(typeof(DataNode));
                if (fieldDefinition.Attribute is DataFieldAttribute dfa)
                {
                    writeExpression = Expression.IfThen(Expression.Not(Expression.Call(
                            mappingDataVar,
                            typeof(MappingDataNode).GetMethod("Has", new[] { typeof(string) })!,
                            Expression.Constant(dfa
                                .Tag))), //check if this node was already written by a type higher up the includetree
                        Expression.Call(
                            mappingDataVar,
                            typeof(MappingDataNode).GetMethod("Add", new[] { typeof(string), typeof(DataNode) })!,
                            Expression.Constant(dfa.Tag),
                            nodeVariable));
                }
                else
                {
                    writeExpression = Expression.IfThenElse(Expression.TypeIs(nodeVariable, typeof(MappingDataNode)),
                        Expression.Call(
                            mappingDataVar,
                            "Insert",
                            Type.EmptyTypes,
                            Expression.Convert(nodeVariable, typeof(MappingDataNode)),
                            Expression.Constant(true)),
                        SerializationManager.ThrowExpression<InvalidOperationException>(
                            $"Writing field {fieldDefinition} for type {typeof(T)} did not return a {nameof(MappingDataNode)} but was annotated to be included."));
                }

                writeExpression = ExpressionUtils.WriteLineBefore($"writing {i}", writeExpression);

                writeExpression = Expression.Block(
                    new[] { nodeVariable },
                    Expression.Assign(valueVar, AccessExpression(i, objParam, fieldDefinition)),
                    Expression.Assign(nodeVariable, call),
                    writeExpression);

                if (fieldDefinition.Attribute is not DataFieldAttribute { Required: true })
                {
                    expressions.Add(Expression.Block(
                        new []{valueVar},
                        Expression.IfThen(
                        Expression.Or(alwaysWriteParam,
                            Expression.Not(
                                IsDefault(i, valueVar, fieldDefinition))),
                        writeExpression)));
                }
                else
                {
                    expressions.Add(
                        Expression.Block(
                            new []{valueVar},
                            writeExpression));
                }

                expressions.Add(ExpressionUtils.WriteLine(i));
            }

            expressions.Add(mappingDataVar);

            return Expression.Lambda<SerializeDelegateSignature>(
                Expression.Block(
                    new []{mappingDataVar},
                    expressions),
                objParam,
                contextParam,
                alwaysWriteParam).Compile();
        }

        private CopyDelegateSignature EmitCopyDelegate(SerializationManager manager)
        {
            var managerConst = Expression.Constant(manager);
            var isServer = manager.DependencyCollection.Resolve<INetManager>().IsServer;

            var sourceParam = Expression.Parameter(typeof(T));
            var targetParam = Expression.Parameter(typeof(T).MakeByRefType());
            var contextParam = Expression.Parameter(typeof(ISerializationContext));
            var skipHookParam = Expression.Parameter(typeof(bool));

            var expressions = new List<Expression>();

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                if (fieldDefinition.Attribute.ServerOnly && !isServer)
                {
                    continue;
                }

                Expression call;
                var sourceVar = Expression.Variable(fieldDefinition.FieldType);
                var targetValue = Expression.Variable(fieldDefinition.FieldType);

                if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Copier)
                {
                    var serializerInstance =
                        manager.GetOrCreateCustomTypeSerializer(fieldDefinition.Attribute.CustomTypeSerializer);
                    var serializerType = typeof(ITypeCopier<>).MakeGenericType(fieldDefinition.FieldType);
                    call = Expression.Assign(targetValue, Expression.Call(
                        Expression.Constant(serializerInstance, serializerType),
                        serializerType.GetMethod("CopyTo")!,
                        managerConst,
                        sourceVar,
                        targetValue,
                        skipHookParam,
                        contextParam));
                }
                else if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].CopyCreator)
                {
                    var serializerInstance =
                        manager.GetOrCreateCustomTypeSerializer(fieldDefinition.Attribute.CustomTypeSerializer);
                    var serializerType = typeof(ITypeCopyCreator<>).MakeGenericType(fieldDefinition.FieldType);
                    call = Expression.Assign(targetValue, Expression.Call(
                        Expression.Constant(serializerInstance, serializerType),
                        serializerType.GetMethod("CreateCopy")!,
                        managerConst,
                        sourceVar,
                        skipHookParam,
                        contextParam));
                }
                else
                {
                    call = Expression.Call(
                        managerConst,
                        "CopyTo",
                        new[] { fieldDefinition.FieldType },
                        sourceVar,
                        targetValue,
                        contextParam,
                        skipHookParam);
                }

                expressions.Add(Expression.Block(
                        new[] { targetValue, sourceVar },
                        Expression.Empty(),
                        Expression.Assign(sourceVar, AccessExpression(i, sourceParam, fieldDefinition)),
                        Expression.Assign(targetValue, Expression.Default(fieldDefinition.FieldType)),
                        call,
                        AssignExpression(i, targetParam, targetValue)
                    )
                );
            }

            return Expression.Lambda<CopyDelegateSignature>(
                Expression.Block(expressions),
                sourceParam,
                targetParam,
                contextParam,
                skipHookParam).Compile();
        }

        private Expression AssignExpression(int i, Expression obj, Expression value)
        {
            return Expression.Invoke(Expression.Constant(FieldAssigners[i]), obj, Expression.Convert(value, typeof(object)));
        }

        private Expression AccessExpression(int i, Expression obj, FieldDefinition fieldDefinition)
        {
            return Expression.Invoke(Expression.Constant(FieldAccessors[i]), obj);
        }

        private Expression IsDefault(int i, Expression left, FieldDefinition fieldDefinition)
        {
            return SerializationManager.EqualExpression(left, Expression.Constant(DefaultValues[i], fieldDefinition.FieldType));
        }
    }
}
