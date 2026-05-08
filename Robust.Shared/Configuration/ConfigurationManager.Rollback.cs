using System;
using System.Collections.Generic;
using System.Linq;
using Nett;

namespace Robust.Shared.Configuration;

internal partial class ConfigurationManager
{
    public void MarkForRollback(params CVarDef[] cVars)
    {
        MarkForRollback(cVars.Select(c => c.Name).ToArray());
    }

    public void MarkForRollback(params string[] cVars)
    {
        var alreadyPending = LoadPendingRollbackTable() ?? [];

        foreach (var cVar in cVars)
        {
            alreadyPending[cVar] = GetCVar(cVar);
        }

        SavePendingRollbackTable(alreadyPending);
    }

    public void UnmarkForRollback(params CVarDef[] cVars)
    {
        UnmarkForRollback(cVars.Select(c => c.Name).ToArray());
    }

    public void UnmarkForRollback(params string[] cVars)
    {
        var alreadyPending = LoadPendingRollbackTable() ?? [];

        foreach (var cVar in cVars)
        {
            alreadyPending.Remove(cVar);
        }

        SavePendingRollbackTable(alreadyPending);
    }

    private void SavePendingRollbackTable(Dictionary<string, object> pending)
    {
        var tbl = SaveToTomlTable(pending.Keys, cVar => pending[cVar]);
        var str = Toml.WriteString(tbl);
        SetCVar(CVars.CfgRollbackData, str);
    }

    public void ApplyRollback()
    {
        var rollbackValue = GetCVar(CVars.CfgRollbackData);

        if (string.IsNullOrWhiteSpace(rollbackValue))
            return;

        _sawmill.Debug("We have CVars to roll back!");

        try
        {
            var tblRoot = Toml.ReadString(rollbackValue);
            var loaded = LoadFromTomlTable(tblRoot);
            _sawmill.Info($"Rolled back CVars: {string.Join(", ", loaded)}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load rollback data:\n{e}");
        }
        finally
        {
            SetCVar(CVars.CfgRollbackData, "");
            SaveToFile();
        }
    }

    private Dictionary<string, object>? LoadPendingRollbackTable()
    {
        var rollbackValue = GetCVar(CVars.CfgRollbackData);

        if (string.IsNullOrWhiteSpace(rollbackValue))
            return null;

        try
        {
            var tblRoot = Toml.ReadString(rollbackValue);
            return ParseCVarValuesFromToml(tblRoot).ToDictionary();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load rollback data:\n{e}");
            return null;
        }
    }
}
