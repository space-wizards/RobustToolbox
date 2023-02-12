using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface.Controllers;

public abstract partial class UIController : IEntityEventSubscriber
{
    protected void SubscribeLocalEvent<T>(
        EntityEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeEvent(EventSource.Local, this, handler, GetType(), before, after);
    }

    protected void SubscribeLocalEvent<T>(
        EntityEventRefHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeEvent(EventSource.Local, this, handler, GetType(), before, after);
    }

    protected void UnSubscribeLocalEvent<T>()
        where T : notnull
    {
        EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Local, this);
    }

    protected void SubscribeNetworkEvent<T>(
        EntitySessionEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeSessionEvent(EventSource.Network, this, handler, GetType(), before, after);
    }

    protected void UnSubscribeNetworkEvent<T>()
        where T : notnull
    {
        EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Network, this);
    }

    protected void SubscribeAllEvent<T>(
        EntitySessionEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeSessionEvent(EventSource.All, this, handler, GetType(), before, after);
    }

    protected void UnSubscribeAllEvent<T>()
        where T : notnull
    {
        EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.All, this);
    }
}
