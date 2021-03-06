﻿using MVR.FileManagementSecure;
using UnityEngine;

public class ProfilesScreen : ScreenBase, IScreen
{
    public const string ScreenName = "Profiles";

    private readonly Storage _storage;

    public ProfilesScreen(EmbodyContext context)
        : base(context)
    {
        _storage = new Storage(context);
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Export and import settings so you can easily re-use them in other scenes or on other atoms.\n\nSave Default Profile will save your settings so that whenever you load this plugin on a new atom, the parameters will be automatically applied.\n\nTo clear your height and body proportions from this instance, e.g. before making a scene public, use Clear Personal Data."), true);

        CreateTitle("Profiles", true);
        var loadPresetUI = CreateButton("Load Profile...", true);
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(SaveFormat.SaveFolder);
            SuperController.singleton.GetMediaPathDialog(
                path =>
                {
                    _storage.LoadProfile(path);
                    screensManager.Show(MainScreen.ScreenName);
                },
                SaveFormat.SaveExt,
                SaveFormat.SaveFolder,
                false,
                true,
                false,
                null,
                false,
                shortcuts);
        });

        var savePresetUI = CreateButton("Save Profile As...", true);
        savePresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
            var fileBrowserUI = SuperController.singleton.fileBrowserUI;
            fileBrowserUI.SetTitle("Save Embody Profile");
            fileBrowserUI.fileRemovePrefix = null;
            fileBrowserUI.hideExtension = false;
            fileBrowserUI.keepOpen = false;
            fileBrowserUI.fileFormat = SaveFormat.SaveExt;
            fileBrowserUI.defaultPath = SaveFormat.SaveFolder;
            fileBrowserUI.showDirs = true;
            fileBrowserUI.shortCuts = null;
            fileBrowserUI.browseVarFilesAsDirectories = false;
            fileBrowserUI.SetTextEntry(true);
            fileBrowserUI.Show(_storage.SaveProfile);
            fileBrowserUI.ActivateFileNameField();
        });

        CreateSpacer(true).height = 40f;
        CreateTitle("Default Profile", true);

        var makeDefaults = CreateButton("Save Default Profile", true);
        makeDefaults.button.onClick.AddListener(_storage.MakeDefault);

        var applyDefaults = CreateButton("Load Default Profile", true);
        applyDefaults.button.onClick.AddListener(() => context.embody.LoadFromDefaults());

        var clearDefaults = CreateButton("Delete Default Profile", true);
        clearDefaults.button.onClick.AddListener(() => FileManagerSecure.DeleteFile(SaveFormat.DefaultsPath));

        CreateSpacer(true).height = 40f;
        CreateTitle("Clear Data", true);

        CreateButton("Clear Personal Data From Plugin", true).button.onClick.AddListener(_storage.ClearPersonalData);
        CreateButton("Reset To Built-In Defaults", true).button.onClick.AddListener(() => Utilities.ResetToDefaults(context));
    }
}
