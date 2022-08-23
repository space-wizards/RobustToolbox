#if !FULL_RELEASE
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Robust.Client.Utility;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.Console.Commands
{
    internal sealed class LauncherAuthCommand : IConsoleCommand
    {
        public string Command => "launchauth";
        public string Description => "Load authentication tokens from launcher data to aid in testing of live servers";
        public string Help => "launchauth [account name]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var wantName = args.Length > 0 ? args[0] : null;

            var basePath = Path.GetDirectoryName(UserDataDir.GetUserDataDir())!;
            var dbPath = Path.Combine(basePath, "launcher", "settings.db");

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

            var cfg = IoCManager.Resolve<IAuthManager>();
            cfg.Token = token;
            cfg.UserId = new NetUserId(userId);

            shell.WriteLine($"Logged into account {userName}");
        }
    }
}

#endif
