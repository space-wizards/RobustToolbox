namespace Robust.Server
{
    public static class ContentStart
    {
        public static void Start(string[] args)
        {
#if FULL_RELEASE
            throw new System.InvalidOperationException("ContentStart is not available on a full release.");
#else
            Program.Start(args, true);
#endif
        }
    }
}
