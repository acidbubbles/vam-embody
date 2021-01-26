using MVR.FileManagementSecure;
using SimpleJSON;

public class ImportExportScreen : ScreenBase, IScreen
{
    public const string ScreenName = "Import / Export";

    private readonly IEmbody _embody;
    private readonly IWorldScaleModule _worldScale;
    private readonly ISnugModule _snug;

    public ImportExportScreen(EmbodyContext context, IEmbody embody, IWorldScaleModule worldScale, ISnugModule snug)
        : base(context)
    {
        _embody = embody;
        _worldScale = worldScale;
        _snug = snug;
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Export all settings so you can easily re-use them in other scenes or on other atoms."), true);

        var loadPresetUI = CreateButton("Import", true);
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(SaveFormat.SaveFolder);
            SuperController.singleton.GetMediaPathDialog(LoadCallback, SaveFormat.SaveExt, SaveFormat.SaveFolder, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Export", true);
        savePresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
            var fileBrowserUI = SuperController.singleton.fileBrowserUI;
            fileBrowserUI.SetTitle("Save Embody preset");
            fileBrowserUI.fileRemovePrefix = null;
            fileBrowserUI.hideExtension = false;
            fileBrowserUI.keepOpen = false;
            fileBrowserUI.fileFormat = SaveFormat.SaveExt;
            fileBrowserUI.defaultPath = SaveFormat.SaveFolder;
            fileBrowserUI.showDirs = true;
            fileBrowserUI.shortCuts = null;
            fileBrowserUI.browseVarFilesAsDirectories = false;
            fileBrowserUI.SetTextEntry(true);
            fileBrowserUI.Show(SaveCallback);
            fileBrowserUI.ActivateFileNameField();
        });

        var makeDefaults = CreateButton("Make Default (Saves)", true);
        makeDefaults.button.onClick.AddListener(MakeDefault);

        var clearDefaults = CreateButton("Clear Default (Saves)", true);
        clearDefaults.button.onClick.AddListener(() => FileManagerSecure.DeleteFile(SaveFormat.DefaultsPath));

        var clearPersonalData = CreateButton("Clear Personal Data (Plugin)", true);
        clearPersonalData.button.onClick.AddListener(() => ClearPersonalData());
    }

    private void SaveCallback(string path)
    {
        SuperController.singleton.fileBrowserUI.fileFormat = null;
        if (string.IsNullOrEmpty(path)) return;
        if (!path.ToLower().EndsWith($".{SaveFormat.SaveExt}")) path += $".{SaveFormat.SaveExt}";
        var jc = context.plugin.GetJSON();
        jc.Remove("id");
        context.plugin.SaveJSON(jc, path);
    }

    private void LoadCallback(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        _embody.activeJSON.val = false;
        var jc = (JSONClass) context.plugin.LoadJSON(path);
        context.plugin.RestoreFromJSON(jc);
    }

    private void MakeDefault()
    {
        FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
        var jc = context.plugin.GetJSON();
        context.plugin.SaveJSON(jc, SaveFormat.DefaultsPath);
    }

    private void ClearPersonalData()
    {
        _embody.activeJSON.val = false;
        _snug.ClearPersonalData();
        _worldScale.ClearPersonalData();
    }
}
