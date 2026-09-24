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
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <para>The least common component should be put in <typeparamref name="TComp1"/> for query performance.</para>
    /// </remarks>
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <para>The least common component should be put in <typeparamref name="TComp1"/> for query performance.</para>
    /// </remarks>
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <typeparam name="TComp4">The fourth component to look for as a tag.</typeparam>
    /// <returns>The singleton entity.</returns>
    /// <exception cref="NonUniqueSingletonException">Thrown when multiple entities match the request.</exception>
    /// <exception cref="MatchNotFoundException">Thrown when no entities match the request.</exception>
    /// <remarks>
    /// <para>The least common component should be put in <typeparamref name="TComp1"/> for query performance.</para>
    /// </remarks>
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
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
    /// <include file='Docs.xml' path='entries/entry[@name="SingletonPauseRemark"]/*'/>
    public bool TrySingle<TComp1, TComp2, TComp3, TComp4>(
        [NotNullWhen(true)] out Entity<TComp1, TComp2, TComp3, TComp4>? entity)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;
}
