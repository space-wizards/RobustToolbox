namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // Use a single counter for RIDs inside Clyde.
        // This way when I mix up RIDs between different systems it blows up quickly. Convenient huh.

        // Also start these at 1 so 0 (uninitialized) is caught easily.
        private long _nextRid = 1;

        private ClydeHandle AllocRid()
        {
            return new(_nextRid++);
        }
    }
}
