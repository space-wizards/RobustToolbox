namespace Robust.Server
{
    public static class ContentStart
    {
        public static void Start(string[] args)
        {
#if FULL_RELEASE
            throw new System.InvalidOperationException("ContentStart.Start is not available on a full release.");
#else
            Program.Start(args, new ServerOptions(), true);
#endif
        }

        public static void StartLibrary(string[] args, ServerOptions options)
        {
            Program.Start(args, options, true);
        }
    }
}
