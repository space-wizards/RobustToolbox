#if TOOLS
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Robust.Client.Utility;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.Console.Commands
{
    internal sealed class LauncherAuthCommand : LocalizedCommands
    {
        [Dependency] private readonly IAuthManager _auth = default!;
        [Dependency] private readonly IGameControllerInternal _gameController = default!;

        public override string Command => "launchauth";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var wantName = args.Length > 0 ? args[0] : null;

            var basePath = UserDataDir.GetRootUserDataDir(_gameController);
            var launcherDirName = Environment.GetEnvironmentVariable("SS14_LAUNCHER_APPDATA_NAME") ?? "launcher";
            var dbPath = Path.Combine(basePath, launcherDirName, "settings.db");

#if USE_SYSTEM_SQLITE
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
#endif
            using var con = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT UserId, UserName, Token FROM Login WHERE Expires > datetime('NOW')";

            if (wantName != null)
            {
                cmd.CommandText += " AND UserName = @userName";
                cmd.Parameters.AddWithValue("@userName", wantName);
            }

            cmd.CommandText += " LIMIT 1;";

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                shell.WriteLine("Unable to find a matching login");
                return;
            }

            var userId = Guid.Parse(reader.GetString(0));
            var userName = reader.GetString(1);
            var token = reader.GetString(2);

            _auth.Token = token;
            _auth.UserId = new NetUserId(userId);

            shell.WriteLine($"Logged into account {userName}");
        }
    }
}

#endif
