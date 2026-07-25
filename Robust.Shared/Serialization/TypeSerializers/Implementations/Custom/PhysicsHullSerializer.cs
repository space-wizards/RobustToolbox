using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

public sealed class PhysicsHullSerializer : ITypeSerializer<Vector2[], SequenceDataNode>
{
    public Vector2[] Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Vector2[]>? instanceProvider = null)
    {
        var vertices = ReadVertices(serializationManager, node, hookCtx, context);
        var hull = PhysicsHull.ComputePoints(vertices, vertices.Length);
        if (hull.Length < 3)
            throw new InvalidMappingException($"Physics hull requires 3-{PhysicsConstants.MaxPolygonVertices} non-collinear vertices.");

        return hull.ToArray();
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Count is < 3 or > PhysicsConstants.MaxPolygonVertices)
            return new ErrorNode(node, $"Physics hull requires 3-{PhysicsConstants.MaxPolygonVertices} vertices.");

        var validations = new List<ValidationNode>(node.Count);
        foreach (var dataNode in node)
        {
            validations.Add(serializationManager.ValidateNode<Vector2>(dataNode, context));
        }

        if (validations.Exists(validation => !validation.Valid))
            return new ValidatedSequenceNode(validations);

        var vertices = ReadVertices(
            serializationManager,
            node,
            SerializationHookContext.ForSkipHooks(false),
            context);

        if (PhysicsHull.ComputePoints(vertices, vertices.Length).Length < 3)
            return new ErrorNode(node, "Physics hull vertices must form a non-collinear convex polygon.");

        return new ValidatedSequenceNode(validations);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        Vector2[] value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode(value.Length);
        foreach (var vertex in value)
        {
            sequence.Add(serializationManager.WriteValue(vertex, alwaysWrite, context));
        }

        return sequence;
    }

    private static Vector2[] ReadVertices(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        SerializationHookContext hookCtx,
        ISerializationContext? context)
    {
        var vertices = new Vector2[node.Count];
        for (var i = 0; i < node.Count; i++)
        {
            vertices[i] = serializationManager.Read<Vector2>(node[i], hookCtx, context);
        }

        return vertices;
    }
}
