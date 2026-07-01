using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Serialization;

public enum NetSizeStatKind
{
    NetMessage,
    EntityEvent,
    Member,
    MemberCount
}

public sealed record NetSizeStat(NetSizeStatKind Kind, string Name, int Value, long Count);

/*
 * Use this for debugging NetMessage (+ EntityEventArgs via MsgEntity) stats and tuning size validation.
 * Toggle net.message_size_stats on first and then run net_size_stats to dump the max stats.
 */
public static class NetSizeStats
{
    private sealed class MutableStat
    {
        public int Bytes;
        public long Count;
    }

    private static readonly ConcurrentDictionary<(NetSizeStatKind Kind, string Name), MutableStat> Stats = new();

    public static bool Enabled { get; set; }

    public static void Clear()
    {
        Stats.Clear();
    }

    public static NetSizeStat[] Snapshot()
    {
        return Stats
            .Select(x => new NetSizeStat(x.Key.Kind, x.Key.Name, x.Value.Bytes, x.Value.Count))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Name)
            .ToArray();
    }

    [Conditional("DEBUG")]
    public static void Record(NetSizeStatKind kind, Type type, int bytes)
    {
        Record(kind, type.FullName ?? type.Name, bytes);
    }

    [Conditional("DEBUG")]
    public static void Record(NetSizeStatKind kind, string name, int bytes)
    {
        if (!Enabled)
            return;

        var stat = Stats.GetOrAdd((kind, name), _ => new MutableStat());
        lock (stat)
        {
            stat.Count++;
            if (bytes > stat.Bytes)
                stat.Bytes = bytes;
        }
    }

    [Conditional("DEBUG")]
    public static void RecordSerializableMembers(object? value, IRobustSerializer serializer)
    {
        if (!Enabled || value == null)
            return;

        var owner = value.GetType();
        foreach (var field in GetFields(owner))
        {
            if (field.IsDefined(typeof(CompilerGeneratedAttribute), false))
                continue;

            RecordMember(owner, field.Name, field.GetValue(value), serializer);
        }

        foreach (var property in GetProperties(owner))
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            RecordMember(owner, property.Name, property.GetValue(value), serializer);
        }
    }

    private static void RecordMember(Type owner, string member, object? value, IRobustSerializer serializer)
    {
        if (value == null)
            return;

        RecordMemberCount(owner, member, value);

        if (!serializer.CanSerialize(value.GetType()))
            return;

        try
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, value);
            Record(NetSizeStatKind.Member, $"{owner.FullName ?? owner.Name}.{member}", (int) stream.Length);
        }
        catch
        {
            // Don't care if stuff dies it's a debug tool.
        }
    }

    private static void RecordMemberCount(Type owner, string member, object value)
    {
        var count = value switch
        {
            string str => str.Length,
            Array array => array.Length,
            System.Collections.ICollection collection => collection.Count,
            _ => -1
        };

        if (count < 0)
            return;

        Record(NetSizeStatKind.MemberCount, $"{owner.FullName ?? owner.Name}.{member}", count);
    }

    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetProperties(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return property;
            }
        }
    }
}
