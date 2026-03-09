using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    // REMARK: No API that allows you to use these queries without them throwing over non-uniqueness should be added.
    //         It's a pretty simple, natural error condition and the game *should* yell about it.

    /// <summary>
    ///     Gets the sole entity with the given component.
    /// </summary>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    public Entity<TComp1> Single<TComp1>()
        where TComp1 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2> Single<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3> Single<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components.
    /// </summary>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The third component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3, TComp4> Single<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, if it exists. Still throws if there's more than one.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <returns>Success.</returns>
    public bool TrySingle<TComp1>([NotNullWhen(true)] out Entity<TComp1>? entity)
        where TComp1 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2>([NotNullWhen(true)] out Entity<TComp1, TComp2>? entity)
        where TComp1 : IComponent
        where TComp2 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2, TComp3>([NotNullWhen(true)] out Entity<TComp1, TComp2, TComp3>? entity)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given components, if one exists, or returns if one does not.
    /// </summary>
    /// <param name="entity">The singleton entity, if any.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <returns>Success.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <remarks>
    ///     The least common component should be put in <typeparamref name="TComp1"/> for query performance.
    /// </remarks>
    public bool TrySingle<TComp1, TComp2, TComp3, TComp4>(
        [NotNullWhen(true)] out Entity<TComp1, TComp2, TComp3, TComp4>? entity)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1> SingleOrSpawn<TComp1>(EntProtoId fallback, MapCoordinates location)
        where TComp1 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2> SingleOrSpawn<TComp1, TComp2>(EntProtoId fallback, MapCoordinates location)
        where TComp1 : IComponent
        where TComp2 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3> SingleOrSpawn<TComp1, TComp2, TComp3>(
        EntProtoId fallback,
        MapCoordinates location)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The fallback prototype to spawn on failure.</param>
    /// <param name="location">The location to spawn the singleton. Nullspace works.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after spawning the fallback.</exception>
    /// <returns>The singleton entity.</returns>
    /// <remarks>
    ///     This does not return the entity it spawns, it tries to look it up, so if spawning that entity violates
    ///     singleton status (or it lacks the necessary component) this will throw immediately after instead of later
    ///     on next call.
    ///
    ///     This will also still throw <see cref="MatchNotFoundException"/> if the spawned entity doesn't match at all.
    /// </remarks>
    public Entity<TComp1, TComp2, TComp3, TComp4> SingleOrSpawn<TComp1, TComp2, TComp3, TComp4>(
        EntProtoId fallback,
        MapCoordinates location)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1> SingleOrInit<TComp1>(Action fallback)
        where TComp1 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2> SingleOrInit<TComp1, TComp2>(Action fallback)
        where TComp1 : IComponent
        where TComp2 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2, TComp3> SingleOrInit<TComp1, TComp2, TComp3>(Action fallback)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent;

    /// <summary>
    ///     Gets the sole entity with the given component, or spawns it at the given location if it doesn't exist.
    /// </summary>
    /// <param name="fallback">The action to call on fallback, should no match exist.</param>
    /// <typeparam name="TComp1">The first component to look for as a tag.</typeparam>
    /// <typeparam name="TComp2">The second component to look for as a tag.</typeparam>
    /// <typeparam name="TComp3">The third component to look for as a tag.</typeparam>
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request after calling fallback.</exception>
    /// <returns>The singleton entity.</returns>
    public Entity<TComp1, TComp2, TComp3, TComp4> SingleOrInit<TComp1, TComp2, TComp3, TComp4>(Action fallback)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;
}
