using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects.EntityBuilders;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

/// <summary>
///     A revised version of <see cref="EntityCreationException"/> with additional metadata.
///     This exception is thrown as part of a <see cref="AggregateException"/> by
///     <see cref="IEntityManager.Spawn(EntityBuilder,System.Nullable{System.Boolean})"/>.
/// </summary>
public sealed class EntityCreationException2 : Exception
{
    public CreationStep Step { get; }
    public EntityBuilder FailureSource { get; }
    public EntProtoId? FailurePrototype => FailureSource.MetaData.EntityPrototype?.ID;

    public override IDictionary Data => new Dictionary<string, object?>()
    {
        { nameof(Step), Step.ToString() },
        { nameof(FailureSource), FailureSource },
        { nameof(FailurePrototype), FailurePrototype },
    };

    /// <summary>
    ///     A non-exhaustive set of steps for entity creation.
    ///     You must handle new cases sanely, it is not a breaking change to add new steps.
    /// </summary>
    public enum CreationStep
    {
        /// <summary>
        ///     Entity allocation and addition.
        /// </summary>
        AllocAdd,
        /// <summary>
        ///     Entity ComponentInit.
        /// </summary>
        Initialize,
        /// <summary>
        ///     Entity ComponentStartup.
        /// </summary>
        Startup,
        /// <summary>
        ///     Entity MapInit.
        /// </summary>
        MapInit,
        /// <summary>
        ///     Post-creation command buffer application.
        /// </summary>
        PostInitCommandBuffer,
        /// <summary>
        ///     Cleanup after a failure elsewhere in spawning.
        /// </summary>
        FailureCleanup,
    }

    public override string Message =>
        $"The entity {FailureSource} failed to be created during step {Step} due to an inner exception.";

    public EntityCreationException2(Exception? innerException, CreationStep step, EntityBuilder failureSource) : base(null, innerException)
    {
        Step = step;
        FailureSource = failureSource;
    }
}
