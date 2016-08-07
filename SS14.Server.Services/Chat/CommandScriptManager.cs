using NLua;
using SS14.Server.Interfaces.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Services.Chat
{
    public class CommandScriptManager : ICommandScriptManager
    {
        private Dictionary<string, string> scriptCache = new Dictionary<string, string>();

        private Lua lua;

        public CommandScriptManager()
        {
            lua = new Lua();
            lua.LoadCLRPackage();
        }

        public void RunFunction(string script)
        {
            string fileName = Path.Combine("Scripts/Commands", script);
            string fileContents = "";

            if (scriptCache.ContainsKey(fileName))
            {
                // Console.WriteLine("Getting {0} from ScriptCache", fileName);
                fileContents = scriptCache[fileName];
            }
            else
            {
                if (!File.Exists(fileName))
                    throw new FileNotFoundException("Unable to find Lua script file!", fileName);

                fileContents = File.ReadAllText(fileName);
                scriptCache.Add(fileName, fileContents);
            }

            lua.DoString(fileContents);
        }

    }
}
