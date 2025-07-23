using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.ViewVariables.Editors;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using CS = System.Runtime.CompilerServices;

namespace Robust.Client.ViewVariables;

internal sealed class ViewVariableControlFactory : IViewVariableControlFactory
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceManager _resManager = default!;
    [Dependency] private readonly IDependencyCollection _dependencyManager = default!;

    private readonly Dictionary<Type, Func<Type, VVPropEditor>> _factoriesByType = new();
    private readonly List<ConditionalViewVariableFactoryMethodContainer> _factoriesWithCondition = new();

    public ViewVariableControlFactory()
    {
        RegisterForType<sbyte>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.SByte));
        RegisterForType<byte>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Byte));
        RegisterForType<ushort>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.UShort));
        RegisterForType<short>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Short));
        RegisterForType<uint>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.UInt));
        RegisterForType<int>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Int));
        RegisterForType<ulong>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.ULong));
        RegisterForType<long>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Long));
        RegisterForType<float>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Float));
        RegisterForType<float>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Float));
        RegisterForType<double>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Double));
        RegisterForType<decimal>(_ =>  new VVPropEditorNumeric(VVPropEditorNumeric.NumberType.Decimal));
        RegisterForType<string>(_ =>  new VVPropEditorString());
        RegisterForType<EntProtoId?>(_ =>  new VVPropEditorNullableEntProtoId());
        RegisterForType<EntProtoId>(_ =>  new VVPropEditorEntProtoId());
        RegisterForType<Vector2>(_ =>  new VVPropEditorVector2(intVec: false));
        RegisterForType<Vector2i>(_ =>  new VVPropEditorVector2(intVec: true));
        RegisterForType<bool>(_ =>  new VVPropEditorBoolean());
        RegisterForType<Angle>(_ =>  new VVPropEditorAngle());
        RegisterForType<Box2>(_ =>  new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.Box2));
        RegisterForType<Box2i>(_ =>  new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.Box2i));
        RegisterForType<UIBox2>(_ =>  new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.UIBox2));
        RegisterForType<UIBox2i>(_ =>  new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.UIBox2i));
        RegisterForType<EntityCoordinates>(_ =>  new VVPropEditorEntityCoordinates());
        RegisterForType<EntityUid>(_ =>  new VVPropEditorEntityUid());
        RegisterForType<NetEntity>(_ =>  new VVPropEditorNetEntity());
        RegisterForType<Color>(_ =>  new VVPropEditorColor());
        RegisterForType<TimeSpan>(_ =>  new VVPropEditorTimeSpan());

        RegisterWithCondition(
            type => type != typeof(ViewVariablesBlobMembers.ServerValueTypeToken) && !type.IsValueType,
            _ => new VVPropEditorReference()
        );
        RegisterWithCondition(
            type => type == typeof(ViewVariablesBlobMembers.ServerKeyValuePairToken)
                    || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>),
            _ => new VVPropEditorKeyValuePair()
        );

        RegisterForAssignableFrom<CS.ITuple>(_ => new VVPropEditorTuple());
        RegisterForAssignableFrom<SoundSpecifier>(_ => new VVPropEditorSoundSpecifier(_protoManager, _resManager));
        RegisterForAssignableFrom<ISelfSerialize>(type => CreateGenericEditor(type, typeof(VVPropEditorISelfSerializable<>)));
        RegisterForAssignableFrom<ViewVariablesBlobMembers.PrototypeReferenceToken>(type => CreateGenericEditor(type, typeof(VVPropEditorIPrototype<>)));
        RegisterForAssignableFrom<IPrototype>(type => CreateGenericEditor(type, typeof(VVPropEditorIPrototype<>)));
        RegisterWithCondition(
            type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ProtoId<>),
            type =>
            {
                var typeArgumentType = type.GenericTypeArguments[0];
                var editor = CreateGenericEditor(typeArgumentType, typeof(VVPropEditorProtoId<>));

                _dependencyManager.InjectDependencies(editor);
                return editor;
            }
        );
        RegisterWithCondition(type => type.IsEnum, _ => new VVPropEditorEnum());
    }

    /// <inheritdoc />
    public void RegisterForType<T>(Func<Type, VVPropEditor> factoryMethod)
    {
        _factoriesByType[typeof(T)] = factoryMethod;
    }

    /// <inheritdoc />
    public void RegisterForAssignableFrom<T>(Func<Type, VVPropEditor> factoryMethod, InsertPosition insertPosition = InsertPosition.First)
    {
        int insertIndex = insertPosition == InsertPosition.Last && _factoriesWithCondition.Count > 0
            ? _factoriesWithCondition.Count - 1
            : 0;
        _factoriesWithCondition.Insert(insertIndex, new(type => typeof(T).IsAssignableFrom(type), factoryMethod));
    }

    /// <inheritdoc />
    public void RegisterWithCondition(Func<Type, bool> condition, Func<Type, VVPropEditor> factory, InsertPosition insertPosition = InsertPosition.First)
    {
        int insertIndex = insertPosition == InsertPosition.Last && _factoriesWithCondition.Count > 0
            ? _factoriesWithCondition.Count - 1
            : 0;
        _factoriesWithCondition.Insert(insertIndex, new(condition, factory));
    }

    /// <inheritdoc />
    public VVPropEditor CreateFor(Type? type)
    {
        if (type == null)
        {
            return new VVPropEditorDummy();
        }

        if (_factoriesByType.TryGetValue(type, out var factory))
        {
            return factory(type);
        }

        foreach (var factoryWithCondition in _factoriesWithCondition)
        {
            if (factoryWithCondition.CanExecute(type))
            {
                return factoryWithCondition.Create(type);
            }
        }

        return new VVPropEditorDummy();
    }

    private static VVPropEditor CreateGenericEditor(Type typeArgumentType, Type genericConrolType)
    {
        var typeToCreate = genericConrolType.MakeGenericType(typeArgumentType);
        return (VVPropEditor)Activator.CreateInstance(typeToCreate)!;
    }

    private sealed record ConditionalViewVariableFactoryMethodContainer(
        Func<Type, bool> CanExecute,
        Func<Type, VVPropEditor> Create
    );
}
