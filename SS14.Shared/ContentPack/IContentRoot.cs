using System.IO;

namespace SS14.Shared.ContentPack
{
    internal interface IContentRoot
    {
        bool Mount();

        MemoryStream GetFile(string relPath);
    }
}
