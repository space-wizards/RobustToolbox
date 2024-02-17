using System;
using System.IO;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Robust.Client.Upload.Commands;

public sealed class UploadFolderCommand : IConsoleCommand
{
    [Dependency] private IResourceManager _resourceManager = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private INetManager _netMan = default!;

    public string Command => "uploadfolder";
    public string Description => Loc.GetString("uploadfolder-command-description");
    public string Help => Loc.GetString("uploadfolder-command-help");

    private static readonly ResPath BaseUploadFolderPath = new("/UploadFolder");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var fileCount = 0;

        if (!_configManager.GetCVar(CVars.ResourceUploadingEnabled))
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-resource-upload-disabled"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-wrong-args"));
            shell.WriteLine( Loc.GetString("uploadfolder-command-help"));
            return;
        }

        var folderPath = BaseUploadFolderPath / args[0];

        if (!_resourceManager.UserData.Exists(folderPath))
        {
            shell.WriteError( Loc.GetString("uploadfolder-command-folder-not-found",("folder", folderPath)));
            return; // bomb out if the folder doesnt exist in /UploadFolder
        }

        //Grab all files in specified folder and upload them
        foreach (var filepath in _resourceManager.UserData.Find($"{folderPath.ToRelativePath()}/").files )
        {
            await using var filestream = _resourceManager.UserData.Open(filepath, FileMode.Open);
            {
                var sizeLimit = _configManager.GetCVar(CVars.ResourceUploadingLimitMb);
                if (sizeLimit > 0f && filestream.Length * SharedNetworkResourceManager.BytesToMegabytes > sizeLimit)
                {
                    shell.WriteError( Loc.GetString("uploadfolder-command-file-too-big", ("filename",filepath), ("sizeLimit",sizeLimit)));
                    return;
                }

                var data = filestream.CopyToArray();

                var msg = new NetworkResourceUploadMessage
                {
                    RelativePath = filepath.RelativeTo(BaseUploadFolderPath),
                    Data = data
                };

                _netMan.ClientSendMessage(msg);
                fileCount++;
            }
        }

        shell.WriteLine( Loc.GetString("uploadfolder-command-success",("fileCount",fileCount)));
    }
}
