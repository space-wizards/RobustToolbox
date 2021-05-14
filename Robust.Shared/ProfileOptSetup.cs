using System.Runtime;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Robust.Shared
{
    internal static class ProfileOptSetup
    {
        public static void Setup(IConfigurationManager cfg)
        {
            // Disabled on non-release since I don't want this to start creating files in Steam's bin directory.
#if FULL_RELEASE
            return;
#endif

            if (!cfg.GetCVar(CVars.SysProfileOpt))
                return;

            ProfileOptimization.SetProfileRoot(PathHelpers.GetExecutableDirectory());
            ProfileOptimization.StartProfile("profile_opt");
        }
    }
}
