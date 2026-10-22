using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Robust.UnitTesting.Pool;

/// <summary>
/// Settings for a server-client pair. These settings may change over a pair's lifetime.
/// The pool manager handles fetching pairs with a given setting, including applying new settings to re-used pairs.
/// </summary>
[Virtual]
public class PairSettings
{
    /// <summary>
    /// Set to true if the test will ruin the server/client pair.
    /// </summary>
    public virtual bool Destructive { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be created fresh.
    /// </summary>
    public virtual bool Fresh { get; init; }

    /// <summary>
    /// Set to true if the given server/client pair should be connected from each other.
    /// Defaults to disconnected as it makes dirty recycling slightly faster.
    /// </summary>
    public virtual bool Connected { get; init; }

    /// <summary>
    /// This will return a server-client pair that has not loaded test prototypes.
    /// Try avoiding this whenever possible, as this will always  create & destroy a new pair.
    /// Use <see cref="RobustTestPair.IsTestPrototype(EntityPrototype)"/> if you need to
    /// exclude test prototypes.
    /// </summary>
    public virtual bool NoLoadTestPrototypes { get; init; }

    /// <summary>
    /// Set this to true to disable the NetInterp CVar on the given server/client pair
    /// </summary>
    public virtual bool DisableInterpolate { get; init; }

    /// <summary>
    /// Set this to true to always clean up the server/client pair before giving it to another borrower
    /// </summary>
    public virtual bool Dirty { get; init; }

    /// <summary>
    /// Overrides the test name detection, and uses this in the test history instead
    /// </summary>
    public virtual string? TestName { get; set; }

    /// <summary>
    /// If set, this will be used to call <see cref="IRobustRandom.SetSeed"/>
    /// </summary>
    public virtual int? ServerSeed { get; set; }

    /// <summary>
    /// If set, this will be used to call <see cref="IRobustRandom.SetSeed"/>
    /// </summary>
    public virtual int? ClientSeed { get; set; }

    #region Inferred Properties

    /// <summary>
    /// If the returned pair must not be reused
    /// </summary>
    public virtual bool MustNotBeReused => Destructive || NoLoadTestPrototypes;

    /// <summary>
    /// If the given pair must be brand new
    /// </summary>
    public virtual bool MustBeNew => Fresh || NoLoadTestPrototypes;

    #endregion

    /// <summary>
    /// Tries to guess if we can skip recycling the server/client pair.
    /// </summary>
    /// <param name="nextSettings">The next set of settings the old pair will be set to</param>
    /// <returns>If we can skip cleaning it up</returns>
    public virtual bool CanFastRecycle(PairSettings nextSettings)
    {
        if (MustNotBeReused)
            throw new InvalidOperationException("Attempting to recycle a non-reusable test.");

        if (nextSettings.MustBeNew)
            throw new InvalidOperationException("Attempting to recycle a test while requesting a fresh test.");

        if (Dirty)
            return false;

        return Connected == nextSettings.Connected;
    }
}
