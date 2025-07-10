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

namespace Robust.Client.ViewVariables;

public sealed class ViewVariableControlFactory : IViewVariableControlFactory
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceManager _resManager = default!;

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

        RegisterWithCondition(type => type.IsEnum, _ => new VVPropEditorEnum());

        RegisterWithCondition(
            type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ProtoId<>),
            type =>
            {
                var editor = (VVPropEditor)Activator.CreateInstance(typeof(VVPropEditorProtoId<>)
                    .MakeGenericType(type.GenericTypeArguments[0]))!;

                IoCManager.InjectDependencies(editor);
                return editor;
            }
        );

        RegisterWithCondition(
            type => typeof(IPrototype).IsAssignableFrom(type) || typeof(ViewVariablesBlobMembers.PrototypeReferenceToken).IsAssignableFrom(type),
            type => (VVPropEditor)Activator.CreateInstance(typeof(VVPropEditorIPrototype<>).MakeGenericType(type))!
        );

        RegisterWithCondition(
            type => typeof(ISelfSerialize).IsAssignableFrom(type),
            type => (VVPropEditor)Activator.CreateInstance(typeof(VVPropEditorISelfSerializable<>).MakeGenericType(type))!
        );

        RegisterWithCondition(type => typeof(SoundSpecifier).IsAssignableFrom(type), _ => new VVPropEditorSoundSpecifier(_protoManager, _resManager));
        RegisterWithCondition(type => type == typeof(ViewVariablesBlobMembers.ServerKeyValuePairToken) ||
                                      type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>), _ => new VVPropEditorKeyValuePair());

        RegisterWithCondition(
            type => type != typeof(ViewVariablesBlobMembers.ServerValueTypeToken) && !type.IsValueType,
            _ => new VVPropEditorReference()
        );
    }

    public void RegisterForType<T>(Func<Type, VVPropEditor> factoryMethod)
    {
        _factoriesByType[typeof(T)] = factoryMethod;
    }

    public void RegisterWithCondition(Func<Type, bool> condition, Func<Type, VVPropEditor> factory)
    {
        _factoriesWithCondition.Add(new ConditionalViewVariableFactoryMethodContainer(condition, factory));
    }

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

    private sealed record ConditionalViewVariableFactoryMethodContainer(
        Func<Type, bool> CanExecute,
        Func<Type, VVPropEditor> Create
    );
}
