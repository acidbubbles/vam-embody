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
        SaveWithFeedback(jc, path, $"Embody: Saved profile '{path}'.", $"profile '{path}'");
    }

    // VaM may ask the user to confirm overwriting a file, so the outcome is only known in these callbacks.
    private void SaveWithFeedback(JSONClass jc, string path, string savedMessage, string what)
    {
        _context.plugin.SaveJSON(
            jc,
            path,
            () => SuperController.LogMessage(savedMessage),
            () => SuperController.LogMessage($"Embody: The {what} was not saved."),
            e => SuperController.LogError($"Embody: Could not save the {what}: {e.Message}"));
    }

    // Reads and validates a profile without touching anything, so a missing or invalid file leaves the current
    // settings intact. Returns null (after logging why) when the file cannot be used.
    public JSONClass ReadProfile(string path)
    {
        path = ResolveProfilePath(path);
        if (string.IsNullOrEmpty(path)) return null;
        var node = _context.plugin.LoadJSON(path);
        var jc = node != null ? node.AsObject : null;
        if (jc == null)
        {
            SuperController.LogError($"Embody: Could not load profile '{path}'. The file is missing or is not valid JSON. Your current settings were kept.");
            return null;
        }
        var version = jc["Version"].AsInt;
        if (version <= 0)
        {
            SuperController.LogError($"Embody: Could not load profile '{path}'. The file is not a valid Embody profile. Your current settings were kept.");
            return null;
        }
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

    // Makes typed paths forgiving: surrounding quotes (as Windows "Copy as path" adds) are removed, and a bare profile
    // name such as "MyProfile" or "MyProfile.embodyprofile" means that profile in the default profiles folder.
    public static string ResolveProfilePath(string path)
    {
        if (path == null) return null;
        path = path.Trim();
        if (path.Length >= 2 && (path[0] == '"' && path[path.Length - 1] == '"' || path[0] == '\'' && path[path.Length - 1] == '\''))
            path = path.Substring(1, path.Length - 2).Trim();
        if (path.Length == 0) return path;
        if (path.IndexOf('/') >= 0 || path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0) return path;
        if (!path.EndsWith($".{SaveFormat.SaveExt}", StringComparison.InvariantCultureIgnoreCase)) path += $".{SaveFormat.SaveExt}";
        return $@"{SaveFormat.SaveFolder}\{path}";
    }

    public void MakeDefault()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = new JSONClass();
        _context.embody.StoreJSON(jc, true, false);
        jc.Remove(_context.diagnostics.storeId);
        SaveWithFeedback(jc, SaveFormat.DefaultsPath, "Embody: Saved your default profile. It is applied whenever Embody is added to a person.", "default profile");
    }

    public void DeleteDefault()
    {
        if (!FileManagerSecure.FileExists(SaveFormat.DefaultsPath))
        {
            SuperController.LogMessage("Embody: There is no default profile to delete.");
            return;
        }
        FileManagerSecure.DeleteFile(
            SaveFormat.DefaultsPath,
            () => SuperController.LogMessage("Embody: Deleted your default profile."),
            () => SuperController.LogMessage("Embody: The default profile was not deleted."),
            e => SuperController.LogError($"Embody: Could not delete the default profile: {e.Message}"));
    }

    public void ClearPersonalData()
    {
        _context.embody.Deactivate();
        _context.snug.ClearPersonalData();
        _context.worldScale.ClearPersonalData();
        _context.trackers.ClearPersonalData();
        SuperController.LogMessage("Embody: Cleared personal data from this instance: player height, world scale method, Snug body sizes and offsets, and tracker offsets.");
    }
}
