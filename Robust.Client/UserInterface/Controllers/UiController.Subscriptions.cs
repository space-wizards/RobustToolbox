using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Client.UserInterface.Controllers;

public abstract partial class UIController : IEntityEventSubscriber
{
    protected void SubscribeLocalEvent<T>(
        EntitySessionEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeSessionEvent(EventSource.Local, this, handler,GetType(), before, after);
    }

    protected void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler,
        Type[]? before = null, Type[]? after = null)
        where TComp : IComponent
        where TEvent : notnull
    {
        EntityManager.EventBus.SubscribeLocalEvent(handler, GetType(), before, after);
    }

    protected void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler,
        Type[]? before = null, Type[]? after = null)
        where TComp : IComponent
        where TEvent : notnull
    {
        EntityManager.EventBus.SubscribeLocalEvent(handler, GetType(), before, after);
    }

    protected void SubscribeNetworkEvent<T>(
        EntitySessionEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeSessionEvent(EventSource.Network, this, handler, GetType(), before, after);
    }

    protected void SubscribeAllEvent<T>(
        EntitySessionEventHandler<T> handler,
        Type[]? before = null, Type[]? after = null)
        where T : notnull
    {
        EntityManager.EventBus.SubscribeSessionEvent(EventSource.All, this, handler, GetType(), before, after);
    }



}
