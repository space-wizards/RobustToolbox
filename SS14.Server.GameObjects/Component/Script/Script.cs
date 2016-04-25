
using System;
using System.Collections.Generic;
using System.IO;

using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using NLua;

namespace SS14.Server.GameObjects
{
	public class ScriptComponent : Component
	{
		// Used for caching files so we don't do file IO for every new script component.
		static Dictionary<string, string> scriptCache = new Dictionary<string, string>();
		private Lua lua;

		public ScriptComponent()
		{
			Family = ComponentFamily.Script;

			lua = new Lua();
			lua.LoadCLRPackage();
		}

		public object[] CallLuaFunction(string funcName, object[] arguments)
		{
			var luaFunc = (LuaFunction)lua[funcName];
			return luaFunc.Call(arguments);
		}

		public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
													 params object[] list)
		{
			ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);
			// Console.WriteLine("Script trigger! {0}", type.ToString());
			if (sender == this)
				return ComponentReplyMessage.Empty;

			string funcName = type.ToString();
			// Console.WriteLine("Script trigger! {0}", funcName);
			if (lua[funcName] != null) // Does this function exist?
				CallLuaFunction(funcName, list);

			return reply;
		}

		public override void SetParameter(ComponentParameter parameter)
		{
			base.SetParameter(parameter);

			switch (parameter.MemberName)
			{
				// Execute code directly.
				case "Code":
					lua.DoString(parameter.GetValue<string>());
					break;

				// Open the .lua file in SS14.Server\Scripts\
				case "File":
					string fileName = Path.Combine("Scripts", parameter.GetValue<string>());
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
					break;
			}
		}

	}
}
