using MVR.FileManagementSecure;
using SimpleJSON;

public class ImportExportScreen : ScreenBase, IScreen
{
    private readonly IEmbody _embody;
    public const string ScreenName = "Import / Export";

    private const string _saveExt = "embodyprofile";
    private const string _saveFolder = "Saves\\embodyprofiles";

    public ImportExportScreen(MVRScript plugin, IEmbody embody)
        : base(plugin)
    {
        _embody = embody;
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Export all settings so you can easily re-use them in other scenes or on other atoms."), true);

        var loadPresetUI = CreateButton("Import", true);
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_saveFolder);
            SuperController.singleton.GetMediaPathDialog((string path) =>
            {
                if (string.IsNullOrEmpty(path)) return;
                _embody.activeJSON.val = false;
                var jc = (JSONClass) plugin.LoadJSON(path);
                plugin.RestoreFromJSON(jc);
            }, _saveExt, _saveFolder, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Export", true);
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
                var jc = plugin.GetJSON();
                jc.Remove("id");
                plugin.SaveJSON(jc, path);
            });
            fileBrowserUI.ActivateFileNameField();
        });
    }
}
