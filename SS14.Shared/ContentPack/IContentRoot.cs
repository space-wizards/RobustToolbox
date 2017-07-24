using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.ContentPack
{
    internal interface IContentRoot
    {
        MemoryStream GetFile(string path);
    }
}
