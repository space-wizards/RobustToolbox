#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

            using var con = GetDb();
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

        public override async ValueTask<CompletionResult> GetCompletionAsync(
            IConsoleShell shell,
            string[] args,
            string argStr,
            CancellationToken cancel)
        {
            if (args.Length != 1)
                return CompletionResult.Empty;

            return await Task.Run(() =>
                {
                    using var con = GetDb();

                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT UserName FROM Login WHERE Expires > datetime('NOW')";

                    var options = new List<CompletionOption>();

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        options.Add(new CompletionOption(name));
                    }

                    return CompletionResult.FromOptions(options);
                },
                cancel);
        }

        private SqliteConnection GetDb()
        {
            var basePath = UserDataDir.GetRootUserDataDir(_gameController);
            var launcherDirName = Environment.GetEnvironmentVariable("SS14_LAUNCHER_APPDATA_NAME") ?? "launcher";
            var dbPath = Path.Combine(basePath, launcherDirName, "settings.db");

#if USE_SYSTEM_SQLITE
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
#endif
            var con = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            con.Open();

            return con;
        }
    }
}

#endif
