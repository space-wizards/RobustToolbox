namespace Robust.UnitTesting.Pool;

public sealed class TestHistoryEntry
{
    /// <summary>
    ///     The name of the test.
    /// </summary>
    public readonly string TestName;

    /// <summary>
    ///     The amount of memory the GC claims to be using at the time of adding this entry.
    /// </summary>
    public readonly long TimeOfUseMemoryTotal;

    internal TestHistoryEntry(string testName, long timeOfUseMemoryTotal)
    {
        TestName = testName;
        TimeOfUseMemoryTotal = timeOfUseMemoryTotal;
    }

    public override string ToString()
    {
        return $"{TestName} (started at {TimeOfUseMemoryTotal} bytes allocated.)";
    }
}
