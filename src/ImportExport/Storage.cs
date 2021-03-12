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
        _context.embody.StoreJSON(jc, true);
        _context.plugin.SaveJSON(jc, path);
    }

    public void LoadProfile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        _context.embody.activeJSON.val = false;
        var jc = _context.plugin.LoadJSON(path).AsObject;
        _context.embody.RestoreFromJSONInternal(jc, false);
    }

    public void MakeDefault()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = new JSONClass();
        _context.embody.StoreJSON(jc, true);
        jc.Remove(_context.diagnostics.storeId);
        _context.plugin.SaveJSON(jc, SaveFormat.DefaultsPath);
    }

    public void ClearPersonalData()
    {
        _context.embody.activeJSON.val = false;
        _context.snug.ClearPersonalData();
        _context.worldScale.ClearPersonalData();
        _context.trackers.ClearPersonalData();
    }
}
