namespace Robust.UnitTesting.Pool;

public sealed class TestHistoryEntry(string testName, long timeOfUseMemoryTotal)
{
    /// <summary>
    ///     The name of the test.
    /// </summary>
    public readonly string TestName = testName;
    /// <summary>
    ///     The amount of memory the GC claims to be using at the time of adding this entry.
    /// </summary>
    public readonly long TimeOfUseMemoryTotal = timeOfUseMemoryTotal;

    public override string ToString()
    {
        return $"{TestName} (started at {TimeOfUseMemoryTotal} bytes allocated.)";
    }
}
