using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    internal partial class DataDefinition<T>
    {
        private PopulateDelegateSignature EmitPopulateDelegate(SerializationManager manager)
        {
            var isServer = manager.DependencyCollection.Resolve<INetManager>().IsServer;

            var managerConst = Expression.Constant(manager);

            var targetParam = Expression.Parameter(typeof(T).MakeByRefType());
            var mappingDataParam = Expression.Parameter(typeof(MappingDataNode));
            var hookCtxParam = Expression.Parameter(typeof(SerializationHookContext));
            var contextParam = Expression.Parameter(typeof(ISerializationContext));

            var expressions = new List<BlockExpression>();

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                if (fieldDefinition.Attribute.ServerOnly && !isServer)
                {
                    continue;
                }

                var isNullable = NullableHelper.IsMarkedAsNullable(fieldDefinition.FieldInfo);

                var nodeVariable = Expression.Variable(typeof(DataNode));
                var valueVariable = Expression.Variable(fieldDefinition.FieldType);
                Expression call;
                if (fieldDefinition.Attribute.CustomTypeSerializer != null && (FieldInterfaceInfos[i].Reader.Value ||
                                                                               FieldInterfaceInfos[i].Reader.Sequence ||
                                                                               FieldInterfaceInfos[i].Reader.Mapping))
                {
                    var switchCases = new List<SwitchCase>();
                    var nullable = fieldDefinition.FieldType.IsNullable();
                    var fieldType = fieldDefinition.FieldType.EnsureNotNullableType();
                    if (FieldInterfaceInfos[i].Reader.Value)
                    {
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void),
                                Expression.Assign(valueVariable, SerializationManager.WrapNullableIfNeededExpression(
                                    Expression.Call(
                                        managerConst,
                                        "Read",
                                        new []{fieldType, typeof(ValueDataNode), fieldDefinition.Attribute.CustomTypeSerializer},
                                        Expression.Convert(nodeVariable, typeof(ValueDataNode)),
                                        hookCtxParam,
                                        contextParam,
                                        Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(fieldType)),
                                        Expression.Constant(!isNullable)), nullable))),
                            Expression.Constant(typeof(ValueDataNode))));
                    }

                    if (FieldInterfaceInfos[i].Reader.Sequence)
                    {
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void),
                                Expression.Assign(valueVariable, SerializationManager.WrapNullableIfNeededExpression(Expression.Call(
                                    managerConst,
                                    "Read",
                                    new []{fieldType, typeof(SequenceDataNode), fieldDefinition.Attribute.CustomTypeSerializer},
                                    Expression.Convert(nodeVariable, typeof(SequenceDataNode)),
                                    hookCtxParam,
                                    contextParam,
                                    Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(fieldType)),
                                    Expression.Constant(!isNullable)), nullable))),
                            Expression.Constant(typeof(SequenceDataNode))));
                    }

                    if (FieldInterfaceInfos[i].Reader.Mapping)
                    {
                        switchCases.Add(Expression.SwitchCase(Expression.Block(typeof(void),
                                Expression.Assign(valueVariable, SerializationManager.WrapNullableIfNeededExpression(Expression.Call(
                                    managerConst,
                                    "Read",
                                    new []{fieldType, typeof(MappingDataNode), fieldDefinition.Attribute.CustomTypeSerializer},
                                    Expression.Convert(nodeVariable, typeof(MappingDataNode)),
                                    hookCtxParam,
                                    contextParam,
                                    Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(fieldType)),
                                    Expression.Constant(!isNullable)), nullable))),
                            Expression.Constant(typeof(MappingDataNode))));
                    }

                    call = Expression.Switch(ExpressionUtils.GetTypeExpression(nodeVariable),
                        ExpressionUtils.ThrowExpression<InvalidOperationException>($"Unable to read node for {fieldDefinition} as valid."),
                        switchCases.ToArray());

                    call = Expression.IfThenElse(
                        Expression.Call(typeof(SerializationManager), "IsNull", Type.EmptyTypes, nodeVariable),
                        isNullable
                            ? Expression.Block(typeof(void),
                                Expression.Assign(valueVariable,
                                    SerializationManager.GetNullExpression(managerConst, fieldType)))
                            : ExpressionUtils.ThrowExpression<NullNotAllowedException>(),
                        call);
                }
                else
                {
                    call = Expression.Assign(valueVariable, Expression.Call(
                        managerConst,
                        "Read",
                        new[] { fieldDefinition.FieldType },
                        nodeVariable,
                        hookCtxParam,
                        contextParam,
                        Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(fieldDefinition.FieldType)),
                        Expression.Constant(!isNullable)));
                }

                call = Expression.Block(
                    new[] { valueVariable },
                    call,
                    AssignIfNotDefaultExpression(i, targetParam, valueVariable));


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
                            ? ExpressionUtils.ThrowExpression<RequiredFieldNotMappedException>(fieldDefinition.FieldType, tagConst, typeof(T))
                            : AssignIfNotDefaultExpression(i, targetParam, Expression.Constant(DefaultValues[i], fieldDefinition.FieldType))
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
                hookCtxParam,
                contextParam).Compile();
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
                    ExpressionUtils.NewExpression<MappingDataNode>()
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

                var isNullable = NullableHelper.IsMarkedAsNullable(fieldDefinition.FieldInfo);

                Expression call;
                var valueVar = Expression.Variable(fieldDefinition.FieldType);
                if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Writer)
                {
                    var fieldType = fieldDefinition.FieldType.EnsureNotNullableType();
                    Expression valueAccess = fieldDefinition.FieldType.IsValueType && isNullable
                        ? Expression.Variable(fieldType)
                        : Expression.Convert(valueVar, fieldType);

                    call = Expression.Call(
                        managerConst,
                        "WriteValue",
                        new[]{fieldType, fieldDefinition.Attribute.CustomTypeSerializer},
                        valueAccess,
                        alwaysWriteParam,
                        contextParam,
                        Expression.Constant(!isNullable));

                    if (fieldDefinition.FieldType.IsValueType && isNullable)
                    {
                        var nodeVar = Expression.Variable(typeof(DataNode));
                        call = Expression.Block(
                            new []{nodeVar},
                            Expression.IfThenElse(
                                SerializationManager.StructNullHasValue(valueVar),
                                Expression.Block(
                                    new[] { (ParameterExpression)valueAccess },
                                    Expression.Assign(valueAccess, Expression.Convert(valueVar, fieldType)),
                                    Expression.Assign(nodeVar, SerializationManager.WrapNullableIfNeededExpression(call, true))),
                                isNullable
                                    ? Expression.Assign(nodeVar, Expression.Constant(ValueDataNode.Null()))
                                    : ExpressionUtils.ThrowExpression<NullNotAllowedException>()),
                            nodeVar);
                    }
                }
                else
                {
                    call = Expression.Call(
                        managerConst,
                        "WriteValue",
                        new[] { fieldDefinition.FieldType },
                        valueVar,
                        alwaysWriteParam,
                        contextParam,
                        Expression.Constant(!isNullable));
                }

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
                        ExpressionUtils.ThrowExpression<InvalidOperationException>(
                            $"Writing field {fieldDefinition} for type {typeof(T)} did not return a {nameof(MappingDataNode)} but was annotated to be included."));
                }

                writeExpression = Expression.Block(
                    new[] { nodeVariable },
                    Expression.Assign(nodeVariable, call),
                    writeExpression);

                if (fieldDefinition.Attribute is not DataFieldAttribute { Required: true })
                {
                    expressions.Add(Expression.Block(
                        new []{valueVar},
                        Expression.Assign(valueVar, AccessExpression(i, objParam)),
                        Expression.IfThen(
                        Expression.OrElse(alwaysWriteParam,
                            Expression.Not(IsDefault(i, valueVar, fieldDefinition))),
                        writeExpression)));
                }
                else
                {
                    expressions.Add(
                        Expression.Block(
                            new []{valueVar},
                            Expression.Assign(valueVar, AccessExpression(i, objParam)),
                            writeExpression));
                }
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
            var hookCtxParam = Expression.Parameter(typeof(SerializationHookContext));

            var expressions = new List<Expression>();

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                if (fieldDefinition.Attribute.ServerOnly && !isServer)
                {
                    continue;
                }

                var isNullable = NullableHelper.IsMarkedAsNullable(fieldDefinition.FieldInfo);

                Expression call;
                if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Copier)
                {
                    var targetValue = Expression.Variable(fieldDefinition.FieldType.EnsureNotNullableType());
                    var finalTargetValue = Expression.Variable(fieldDefinition.FieldType);
                    var fieldType = fieldDefinition.FieldType.EnsureNotNullableType();

                    var sourceAccess = fieldType.IsValueType && isNullable
                        ? Expression.Variable(fieldType)
                        : AccessExpression(i, sourceParam);

                    call = Expression.Block(
                        Expression.Call(
                        managerConst,
                        "CopyTo",
                        new[]{fieldType, fieldDefinition.Attribute.CustomTypeSerializer},
                        sourceAccess,
                        targetValue,
                        hookCtxParam,
                        contextParam,
                        Expression.Constant(!isNullable)),
                        Expression.Assign(finalTargetValue, Expression.Convert(targetValue, fieldDefinition.FieldType)));

                    //null check for non-value types is handled in copyto. we are just making sure the types match up
                    if (isNullable && fieldType.IsValueType)
                    {
                        var sourceValue = Expression.Variable(fieldDefinition.FieldType);
                        call = Expression.Block(
                            new[] { sourceValue, (ParameterExpression)sourceAccess },
                            Expression.Assign(sourceValue, AccessExpression(i, sourceParam)),
                            Expression.IfThenElse(SerializationManager.StructNullHasValue(sourceValue),
                                Expression.Block(
                                    Expression.Assign(sourceAccess, Expression.Convert(sourceValue, fieldType)),
                                    call),
                                Expression.Assign(finalTargetValue,
                                    SerializationManager.GetNullExpression(managerConst, fieldType))));
                    }

                    call = Expression.Block(
                        new[] { finalTargetValue, targetValue },
                        Expression.Assign(targetValue, manager.InstantiationExpression(managerConst, fieldDefinition.FieldType.EnsureNotNullableType())),
                        call,
                        finalTargetValue);
                }
                else if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].CopyCreator)
                {
                    call = Expression.Call(
                        managerConst,
                        "CreateCopy",
                        new []{fieldDefinition.FieldType, fieldDefinition.Attribute.CustomTypeSerializer},
                        AccessExpression(i, sourceParam),
                        hookCtxParam,
                        contextParam,
                        Expression.Constant(!isNullable));
                }
                else
                {
                    call = Expression.Call(
                        managerConst,
                        "CreateCopy",
                        new[] { fieldDefinition.FieldType },
                        AccessExpression(i, sourceParam),
                        hookCtxParam,
                        contextParam,
                        Expression.Constant(!isNullable));
                }

                expressions.Add(AssignIfNotDefaultExpression(i, targetParam, call));
            }

            return Expression.Lambda<CopyDelegateSignature>(
                Expression.Block(expressions),
                sourceParam,
                targetParam,
                hookCtxParam,
                contextParam).Compile();
        }

        private ValidateFieldDelegate EmitFieldValidationDelegate(SerializationManager manager, int i)
        {
            var managerConst = Expression.Constant(manager);

            var nodeParam = Expression.Parameter(typeof(DataNode));
            var contextParam = Expression.Parameter(typeof(ISerializationContext));

            var field = BaseFieldDefinitions[i];
            var interfaceInfo = FieldInterfaceInfos[i];

            var fieldType = field.FieldType.EnsureNotNullableType();

            var switchCases = new List<SwitchCase>();
            if (interfaceInfo.Validator.Value)
            {
                switchCases.Add(Expression.SwitchCase(
                    Expression.Call(
                        managerConst,
                        "ValidateNode",
                        new []{fieldType, typeof(ValueDataNode), field.Attribute.CustomTypeSerializer!},
                        Expression.Convert(nodeParam, typeof(ValueDataNode)),
                        contextParam),
                    Expression.Constant(typeof(ValueDataNode))));
            }

            if (interfaceInfo.Validator.Sequence)
            {
                switchCases.Add(Expression.SwitchCase(
                    Expression.Call(
                        managerConst,
                        "ValidateNode",
                        new []{fieldType, typeof(SequenceDataNode), field.Attribute.CustomTypeSerializer!},
                        Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                        contextParam),
                    Expression.Constant(typeof(SequenceDataNode))));
            }

            if (interfaceInfo.Validator.Mapping)
            {
                switchCases.Add(Expression.SwitchCase(
                    Expression.Call(
                        managerConst,
                        "ValidateNode",
                        new []{fieldType, typeof(MappingDataNode), field.Attribute.CustomTypeSerializer!},
                        Expression.Convert(nodeParam, typeof(MappingDataNode)),
                        contextParam),
                    Expression.Constant(typeof(MappingDataNode))));
            }

            var @switch = Expression.Switch(ExpressionUtils.GetTypeExpression(nodeParam),
                Expression.Call(
                    managerConst,
                    "ValidateNode",
                    new[] { fieldType },
                    nodeParam,
                    contextParam),
                switchCases.ToArray());

            return Expression.Lambda<ValidateFieldDelegate>(
                @switch,
                nodeParam,
                contextParam).Compile();
        }

        private Expression AssignIfNotDefaultExpression(int i, Expression obj, Expression value)
        {
            var assigner = FieldAssigners[i];
            Expression assignerExpr;

            if (assigner is FieldInfo fieldInfo)
                assignerExpr = Expression.Assign(Expression.Field(obj, fieldInfo), value);
            else if (assigner is MethodInfo methodInfo)
                assignerExpr = Expression.Call(obj, methodInfo, value);
            else
                assignerExpr = Expression.Invoke(Expression.Constant(assigner), obj, value);

            return Expression.IfThen(
                Expression.Not(ExpressionUtils.EqualExpression(
                    Expression.Constant(DefaultValues[i], BaseFieldDefinitions[i].FieldType), value)),
                assignerExpr);
        }

        private Expression AccessExpression(int i, Expression obj)
        {
            var accessor = FieldAccessors[i];
            if (accessor is FieldInfo fieldInfo)
                return Expression.Field(obj, fieldInfo);

            if (accessor is MethodInfo methodInfo)
                return Expression.Call(obj, methodInfo);

            return Expression.Invoke(Expression.Constant(accessor), obj);
        }

        private Expression IsDefault(int i, Expression left, FieldDefinition fieldDefinition)
        {
            return ExpressionUtils.EqualExpression(left, Expression.Constant(DefaultValues[i], fieldDefinition.FieldType));
        }
    }
}
