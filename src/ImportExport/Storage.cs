using System;
using MVR.FileManagementSecure;
using SimpleJSON;

public class Storage
{
    private readonly EmbodyContext _context;

    public Storage(EmbodyContext context)
    {
        _context = context;
    }

    public void SaveProfile(string path)
    {
        SuperController.singleton.fileBrowserUI.fileFormat = null;
        if (string.IsNullOrEmpty(path)) return;
        if (!path.EndsWith($".{SaveFormat.SaveExt}", StringComparison.InvariantCultureIgnoreCase)) path += $".{SaveFormat.SaveExt}";
        var jc = new JSONClass();
        _context.embody.StoreJSON(jc, true, false);
        _context.plugin.SaveJSON(jc, path);
    }

    public bool LoadProfile(string path)
    {
        var jc = ReadProfile(path);
        if (jc == null) return false;
        ApplyProfile(jc, path);
        return true;
    }

    // Reads and validates a profile without touching anything, so a missing or invalid file leaves the current
    // settings intact. Returns null (after logging why) when the file cannot be used.
    public JSONClass ReadProfile(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var node = _context.plugin.LoadJSON(path);
        var jc = node != null ? node.AsObject : null;
        if (jc == null)
        {
            SuperController.LogError($"Embody: Could not load profile '{path}'. The file is missing or is not valid JSON. Your current settings were kept.");
            return null;
        }
        var version = jc["Version"].AsInt;
        if (version < 2 || version > SaveFormat.Version)
        {
            SuperController.LogError($"Embody: Could not load profile '{path}'. Its save format ({version}) is not supported by this version of Embody (supports 2 to {SaveFormat.Version}). Your current settings were kept.");
            return null;
        }
        return jc;
    }

    public void ApplyProfile(JSONClass jc, string path)
    {
        _context.embody.Deactivate();
        Utilities.ResetToDefaults(_context);
        _context.embody.RestoreFromJSONInternal(jc, true, false);
        SuperController.LogMessage($"Embody: Loaded profile '{path}'.");
    }

    public void MakeDefault()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = new JSONClass();
        _context.embody.StoreJSON(jc, true, false);
        jc.Remove(_context.diagnostics.storeId);
        _context.plugin.SaveJSON(jc, SaveFormat.DefaultsPath);
    }

    public void ClearPersonalData()
    {
        _context.embody.Deactivate();
        _context.snug.ClearPersonalData();
        _context.worldScale.ClearPersonalData();
        _context.trackers.ClearPersonalData();
    }
}
