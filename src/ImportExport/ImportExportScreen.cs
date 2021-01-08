public class ImportExportScreen : ScreenBase, IScreen
{
    public const string ScreenName = "Import / Export";

    public ImportExportScreen(MVRScript plugin)
        : base(plugin)
    {
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Export all settings so you can easily re-use them in other scenes or on other atoms."), true);
        // InitPresetUI();
    }

    /*
    private void InitPresetUI()
    {
        var loadPresetUI = CreateButton("Load Preset", false);
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_saveFolder);
            SuperController.singleton.GetMediaPathDialog((string path) =>
            {
                if (string.IsNullOrEmpty(path)) return;
                JSONClass jc = (JSONClass) LoadJSON(path);
                RestoreFromJSON(jc);
                SyncSelectedAnchorJSON("");
                SyncHandsOffset();
            }, _saveExt, _saveFolder, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Save Preset", false);
        savePresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var fileBrowserUI = SuperController.singleton.fileBrowserUI;
            fileBrowserUI.SetTitle("Save colliders preset");
            fileBrowserUI.fileRemovePrefix = null;
            fileBrowserUI.hideExtension = false;
            fileBrowserUI.keepOpen = false;
            fileBrowserUI.fileFormat = _saveExt;
            fileBrowserUI.defaultPath = _saveFolder;
            fileBrowserUI.showDirs = true;
            fileBrowserUI.shortCuts = null;
            fileBrowserUI.browseVarFilesAsDirectories = false;
            fileBrowserUI.SetTextEntry(true);
            fileBrowserUI.Show((string path) =>
            {
                fileBrowserUI.fileFormat = null;
                if (string.IsNullOrEmpty(path)) return;
                if (!path.ToLower().EndsWith($".{_saveExt}")) path += $".{_saveExt}";
                var jc = GetJSON();
                jc.Remove("id");
                SaveJSON(jc, path);
            });
            fileBrowserUI.ActivateFileNameField();
        });
    }
    */
}
