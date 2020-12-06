using System;

namespace Robust.Shared.ContentPack
{
    public static class ModLoaderExt
    {
        public static bool IsContentType(this IModLoader modLoader, Type type)
        {
            // It should be noted that this method is circumventable IF content could inherit Type.
            // it cannot, luckily.
            return modLoader.IsContentAssembly(type.Assembly);
        }

        public static bool IsContentTypeAccessAllowed(this IModLoader modLoader, Type type)
        {
            return modLoader.IsContentType(type);
        }
    }
}
