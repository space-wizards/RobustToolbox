using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    // TODO POOLING
    // Just add overrides that take in an existing collection.

    /// <summary>
    /// Inverse lookup for net entities.
    /// Regular lookup uses MetadataComponent.
    /// </summary>
    protected readonly Dictionary<NetEntity, (EntityUid, MetaDataComponent)> NetEntityLookup = new(EntityCapacity);

    /// <summary>
    /// Clears an old inverse lookup for a particular entityuid.
    /// Do not call this unless you are sure of what you're doing.
    /// </summary>
    internal void ClearNetEntity(NetEntity netEntity)
    {
        NetEntityLookup.Remove(netEntity);
    }

    /// <summary>
    /// Set the inverse lookup for a particular entityuid.
    /// Do not call this unless you are sure of what you're doing.
    /// </summary>
    internal void SetNetEntity(EntityUid uid, NetEntity netEntity, MetaDataComponent component)
    {
        DebugTools.Assert(component.NetEntity == NetEntity.Invalid || _netMan.IsClient);
        DebugTools.Assert(!NetEntityLookup.ContainsKey(netEntity));
        NetEntityLookup[netEntity] = (uid, component);
        component.NetEntity = netEntity;
    }

    /// <inheritdoc />
    public virtual bool IsClientSide(EntityUid uid, MetaDataComponent? metadata = null)
    {
        return false;
    }

    #region NetEntity

    /// <inheritdoc />
    public bool TryParseNetEntity(string arg, [NotNullWhen(true)] out EntityUid? entity)
    {
        if (!NetEntity.TryParse(arg, out var netEntity) ||
            !TryGetEntity(netEntity, out entity))
        {
            entity = null;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool TryGetEntity(NetEntity nEntity, [NotNullWhen(true)] out EntityUid? entity)
    {
        if (NetEntityLookup.TryGetValue(nEntity, out var went))
        {
            entity = went.Item1;
            return true;
        }

        entity = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetEntityData(NetEntity nEntity, [NotNullWhen(true)] out EntityUid? entity, [NotNullWhen(true)] out MetaDataComponent? meta)
    {
        if (NetEntityLookup.TryGetValue(nEntity, out var went))
        {
            entity = went.Item1;
            meta = went.Item2;
            return true;
        }

        entity = null;
        meta = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetEntity(NetEntity? nEntity, [NotNullWhen(true)] out EntityUid? entity)
    {
        if (nEntity == null)
        {
            entity = null;
            return false;
        }

        return TryGetEntity(nEntity.Value, out entity);
    }

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null)
    {
        if (uid == EntityUid.Invalid)
        {
            netEntity = null;
            return false;
        }

        // TODO NetEntity figure out why this happens
        // I wanted this to logMissing but it seems to break a loootttt of dodgy stuff on content.
        if (MetaQuery.Resolve(uid, ref metadata, false))
        {
            netEntity = metadata.NetEntity;
            return true;
        }

        netEntity = NetEntity.Invalid;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetNetEntity(EntityUid? uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null)
    {
        if (uid == null)
        {
            netEntity = null;
            return false;
        }

        return TryGetNetEntity(uid.Value, out netEntity, metadata);
    }

    /// <inheritdoc />
    public virtual EntityUid EnsureEntity<T>(NetEntity nEntity, EntityUid callerEntity)
    {
        // On server we don't want to ensure any reserved entities for later or flag for comp state handling
        // so this is just GetEntity. Client-side code overrides this method.
        return GetEntity(nEntity);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid? EnsureEntity<T>(NetEntity? nEntity, EntityUid callerEntity)
    {
        if (nEntity == null)
            return null;

        return EnsureEntity<T>(nEntity.Value, callerEntity);
    }

    /// <inheritdoc />
    public EntityUid GetEntity(NetEntity nEntity)
    {
        if (nEntity == NetEntity.Invalid)
            return EntityUid.Invalid;

        if (!NetEntityLookup.TryGetValue(nEntity, out var tuple))
            return EntityUid.Invalid;

        return tuple.Item1;
    }

    public (EntityUid, MetaDataComponent) GetEntityData(NetEntity nEntity)
    {
        return NetEntityLookup[nEntity];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityUid? GetEntity(NetEntity? nEntity)
    {
        if (nEntity == null)
            return null;

        return GetEntity(nEntity.Value);
    }

    /// <inheritdoc />
    public NetEntity GetNetEntity(EntityUid uid, MetaDataComponent? metadata = null)
    {
        if (uid == EntityUid.Invalid)
            return NetEntity.Invalid;

        if (!MetaQuery.Resolve(uid, ref metadata))
            return NetEntity.Invalid;

        return metadata.NetEntity;
    }

    /// <inheritdoc />
    public NetEntity? GetNetEntity(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        if (uid == null)
            return null;

        return GetNetEntity(uid.Value, metadata);
    }

    #endregion

    #region NetCoordinates

    /// <inheritdoc />
    public NetCoordinates GetNetCoordinates(EntityCoordinates coordinates, MetaDataComponent? metadata = null)
    {
        return new NetCoordinates(GetNetEntity(coordinates.EntityId, metadata), coordinates.Position);
    }

    /// <inheritdoc />
    public NetCoordinates? GetNetCoordinates(EntityCoordinates? coordinates, MetaDataComponent? metadata = null)
    {
        if (coordinates == null)
            return null;

        return new NetCoordinates(GetNetEntity(coordinates.Value.EntityId, metadata), coordinates.Value.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates GetCoordinates(NetCoordinates coordinates)
    {
        return new EntityCoordinates(GetEntity(coordinates.NetEntity), coordinates.Position);
    }

    /// <inheritdoc />
    public EntityCoordinates? GetCoordinates(NetCoordinates? coordinates)
    {
        if (coordinates == null)
            return null;

        return new EntityCoordinates(GetEntity(coordinates.Value.NetEntity), coordinates.Value.Position);
    }

    /// <inheritdoc />
    public virtual EntityCoordinates EnsureCoordinates<T>(NetCoordinates netCoordinates, EntityUid callerEntity)
    {
        // See EnsureEntity
        return GetCoordinates(netCoordinates);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCoordinates? EnsureCoordinates<T>(NetCoordinates? netCoordinates, EntityUid callerEntity)
    {
        if (netCoordinates == null)
            return null;

        return EnsureCoordinates<T>(netCoordinates.Value, callerEntity);
    }

    #endregion

    #region Collection helpers

    /// <inheritdoc />
    public HashSet<EntityUid> GetEntitySet(HashSet<NetEntity> netEntities)
    {
        var entities = new HashSet<EntityUid>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid> GetEntityList(List<NetEntity> netEntities)
    {
        var entities = new List<EntityUid>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    public Dictionary<EntityUid, T> GetEntityDictionary<T>(Dictionary<NetEntity, T> netEntities)
    {
        var entities = new Dictionary<EntityUid, T>(netEntities.Count);

        foreach (var pair in netEntities)
        {
            entities.Add(GetEntity(pair.Key), pair.Value);
        }

        return entities;
    }

    public Dictionary<T, EntityUid> GetEntityDictionary<T>(Dictionary<T, NetEntity> netEntities) where T : notnull
    {
        var entities = new Dictionary<T, EntityUid>(netEntities.Count);

        foreach (var pair in netEntities)
        {
            entities.Add(pair.Key, GetEntity(pair.Value));
        }

        return entities;
    }

    public Dictionary<T, EntityUid?> GetEntityDictionary<T>(Dictionary<T, NetEntity?> netEntities) where T : notnull
    {
        var entities = new Dictionary<T, EntityUid?>(netEntities.Count);

        foreach (var pair in netEntities)
        {
            entities.Add(pair.Key, GetEntity(pair.Value));
        }

        return entities;
    }

    public Dictionary<EntityUid, EntityUid> GetEntityDictionary(Dictionary<NetEntity, NetEntity> netEntities)
    {
        var entities = new Dictionary<EntityUid, EntityUid>(netEntities.Count);

        foreach (var pair in netEntities)
        {
            entities.Add(GetEntity(pair.Key), GetEntity(pair.Value));
        }

        return entities;
    }

    public Dictionary<EntityUid, EntityUid?> GetEntityDictionary(Dictionary<NetEntity, NetEntity?> netEntities)
    {
        var entities = new Dictionary<EntityUid, EntityUid?>(netEntities.Count);

        foreach (var pair in netEntities)
        {
            entities.Add(GetEntity(pair.Key), GetEntity(pair.Value));
        }

        return entities;
    }

    public HashSet<EntityUid> EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity)
    {
        var entities = new HashSet<EntityUid>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }

        return entities;
    }

    public void EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity, HashSet<EntityUid> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }
    }

    /// <inheritdoc />
    public List<EntityUid> EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity)
    {
        var entities = new List<EntityUid>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }

        return entities;
    }

    public void EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity, List<EntityUid> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var netEntity in netEntities)
        {
            entities.Add(EnsureEntity<T>(netEntity, callerEntity));
        }
    }

    public void EnsureEntityDictionary<TComp, TValue>(Dictionary<NetEntity, TValue> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, TValue> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(EnsureEntity<TComp>(pair.Key, callerEntity), pair.Value);
        }
    }

    public void EnsureEntityDictionaryNullableValue<TComp, TValue>(Dictionary<NetEntity, TValue?> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, TValue?> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(EnsureEntity<TComp>(pair.Key, callerEntity), pair.Value);
        }
    }

    public void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity> netEntities, EntityUid callerEntity,
        Dictionary<TKey, EntityUid> entities) where TKey : notnull
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(pair.Key, EnsureEntity<TComp>(pair.Value, callerEntity));
        }
    }

    public void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity?> netEntities, EntityUid callerEntity,
        Dictionary<TKey, EntityUid?> entities) where TKey : notnull
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(pair.Key, EnsureEntity<TComp>(pair.Value, callerEntity));
        }
    }

    public void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, EntityUid> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(EnsureEntity<TComp>(pair.Key, callerEntity), EnsureEntity<TComp>(pair.Value, callerEntity));
        }
    }

    public void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity?> netEntities, EntityUid callerEntity,
        Dictionary<EntityUid, EntityUid?> entities)
    {
        entities.Clear();
        entities.EnsureCapacity(netEntities.Count);
        foreach (var pair in netEntities)
        {
            entities.TryAdd(EnsureEntity<TComp>(pair.Key, callerEntity), EnsureEntity<TComp>(pair.Value, callerEntity));
        }
    }

    /// <inheritdoc />
    public List<EntityUid> GetEntityList(ICollection<NetEntity> netEntities)
    {
        var entities = new List<EntityUid>(netEntities.Count);
        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityUid?> GetEntityList(List<NetEntity?> netEntities)
    {
        var entities = new List<EntityUid?>(netEntities.Count);

        foreach (var netEntity in netEntities)
        {
            entities.Add(GetEntity(netEntity));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid[] GetEntityArray(NetEntity[] netEntities)
    {
        var entities = new EntityUid[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityUid?[] GetEntityArray(NetEntity?[] netEntities)
    {
        var entities = new EntityUid?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetEntity(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetEntity> GetNetEntitySet(HashSet<EntityUid> entities)
    {
        var newSet = new HashSet<NetEntity>(entities.Count);

        foreach (var ent in entities)
        {
            MetaQuery.TryGetComponent(ent, out var metadata);
            newSet.Add(GetNetEntity(ent, metadata));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetEntity> GetNetEntityList(List<EntityUid> entities)
    {
        var netEntities = new List<NetEntity>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity> GetNetEntityList(IReadOnlyList<EntityUid> entities)
    {
        var netEntities = new List<NetEntity>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity> GetNetEntityList(ICollection<EntityUid> entities)
    {
        var netEntities = new List<NetEntity>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetEntity?> GetNetEntityList(List<EntityUid?> entities)
    {
        var netEntities = new List<NetEntity?>(entities.Count);

        foreach (var netEntity in entities)
        {
            netEntities.Add(GetNetEntity(netEntity));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity[] GetNetEntityArray(EntityUid[] entities)
    {
        var netEntities = new NetEntity[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetEntity?[] GetNetEntityArray(EntityUid?[] entities)
    {
        var netEntities = new NetEntity?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetEntity(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public Dictionary<NetEntity, T> GetNetEntityDictionary<T>(Dictionary<EntityUid, T> entities)
    {
        var netEntities = new Dictionary<NetEntity, T>(entities.Count);

        foreach (var pair in entities)
        {
            netEntities.Add(GetNetEntity(pair.Key), pair.Value);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public Dictionary<T, NetEntity> GetNetEntityDictionary<T>(Dictionary<T, EntityUid> entities) where T : notnull
    {
        var netEntities = new Dictionary<T, NetEntity>(entities.Count);

        foreach (var pair in entities)
        {
            netEntities.Add(pair.Key, GetNetEntity(pair.Value));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public Dictionary<T, NetEntity?> GetNetEntityDictionary<T>(Dictionary<T, EntityUid?> entities) where T : notnull
    {
        var netEntities = new Dictionary<T, NetEntity?>(entities.Count);

        foreach (var pair in entities)
        {
            netEntities.Add(pair.Key, GetNetEntity(pair.Value));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public Dictionary<NetEntity, NetEntity> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid> entities)
    {
        var netEntities = new Dictionary<NetEntity, NetEntity>(entities.Count);

        foreach (var pair in entities)
        {
            netEntities.Add(GetNetEntity(pair.Key), GetNetEntity(pair.Value));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public Dictionary<NetEntity, NetEntity?> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid?> entities)
    {
        var netEntities = new Dictionary<NetEntity, NetEntity?>(entities.Count);

        foreach (var pair in entities)
        {
            netEntities.Add(GetNetEntity(pair.Key), GetNetEntity(pair.Value));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public HashSet<EntityCoordinates> GetEntitySet(HashSet<NetCoordinates> netEntities)
    {
        var entities = new HashSet<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> GetEntityList(List<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates> GetEntityList(ICollection<NetCoordinates> netEntities)
    {
        var entities = new List<EntityCoordinates>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public List<EntityCoordinates?> GetEntityList(List<NetCoordinates?> netEntities)
    {
        var entities = new List<EntityCoordinates?>(netEntities.Count);

        foreach (var netCoordinates in netEntities)
        {
            entities.Add(GetCoordinates(netCoordinates));
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates[] GetEntityArray(NetCoordinates[] netEntities)
    {
        var entities = new EntityCoordinates[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public EntityCoordinates?[] GetEntityArray(NetCoordinates?[] netEntities)
    {
        var entities = new EntityCoordinates?[netEntities.Length];

        for (var i = 0; i < netEntities.Length; i++)
        {
            entities[i] = GetCoordinates(netEntities[i]);
        }

        return entities;
    }

    /// <inheritdoc />
    public HashSet<NetCoordinates> GetNetCoordinatesSet(HashSet<EntityCoordinates> entities)
    {
        var newSet = new HashSet<NetCoordinates>(entities.Count);

        foreach (var coordinates in entities)
        {
            newSet.Add(GetNetCoordinates(coordinates));
        }

        return newSet;
    }

    /// <inheritdoc />
    public List<NetCoordinates> GetNetCoordinatesList(List<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates> GetNetCoordinatesList(ICollection<EntityCoordinates> entities)
    {
        var netEntities = new List<NetCoordinates>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public List<NetCoordinates?> GetNetCoordinatesList(List<EntityCoordinates?> entities)
    {
        var netEntities = new List<NetCoordinates?>(entities.Count);

        foreach (var netCoordinates in entities)
        {
            netEntities.Add(GetNetCoordinates(netCoordinates));
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates[] GetNetCoordinatesArray(EntityCoordinates[] entities)
    {
        var netEntities = new NetCoordinates[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    /// <inheritdoc />
    public NetCoordinates?[] GetNetCoordinatesArray(EntityCoordinates?[] entities)
    {
        var netEntities = new NetCoordinates?[entities.Length];

        for (var i = 0; i < entities.Length; i++)
        {
            netEntities[i] = GetNetCoordinates(entities[i]);
        }

        return netEntities;
    }

    #endregion
}
