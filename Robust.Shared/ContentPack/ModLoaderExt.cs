using System;

namespace Robust.Shared.ContentPack
{
    public static class ModLoaderExt
    {
        public static bool IsContentTypeAccessAllowed(this IModLoader modLoader, Type type)
        {
            return modLoader.IsContentAssembly(type.Assembly);
        }
    }
}
