using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainScreen : ScreenBase, IScreen
{
    private readonly IEmbodyModule[] _modules;
    private readonly WizardModule _wizard;
    public const string ScreenName = "Embody (Main)";

    public MainScreen(EmbodyContext context, IEmbodyModule[] modules, WizardModule wizard)
        : base(context)
    {
        _modules = modules;
        _wizard = wizard;
    }

    public void Show()
    {
        CreateSpacer().height = 10f;
        CreateButton($"Import / Export Settings...").button.onClick.AddListener(() => screensManager.Show(ImportExportScreen.ScreenName));
        CreateButton("Setup Wizard").button.onClick.AddListener(() => screensManager.Show(WizardScreen.ScreenName));

        CreateText(new JSONStorableString("", @"
Welcome to <b>Embody</b>! This plugin improves possession on many levels. Select a mode, run the wizard and select the Active toggle to start!

Scroll for more information.

There are three presets:

<b>Native VaM possession</b>

This is the normal Virt-A-Mate possession, though head geometry will be hidden and the camera will be adjusted. You need to activate VaM's possession to use this.

<b>Improved possession</b>

This is the default and recommended preset for most cases. Builds on the previous features. This will adjust eyes ""look at"" so looking at mirrors will look back, and adjust the world scale automatically. Use the Setup Wizard to measure your height, otherwise eye distance will be used instead.

<b>Snug</b>

This further builds on previous features, but also adds body proportion adjustments to hands, so touching your own body will reflect correctly in the VR world. You must run the Setup Wizard after selecting this mode.

<b>Passenger</b>

Instead of you possessing the VR model, the VR model will ""possess you"". This means that if the VR model is moving, e.g. with Timeline or animation patterns, your VR view will move. This can be an extremely satisfying experience, but can also make some people sick. Only try this if you are comfortable with VR camera movement.
".Trim()), true);

        var presetsJSON = new JSONStorableStringChooser("Presets", new List<string>
        {
            "Native VaM possession",
            "Improved possession",
            "Snug",
            "Passenger",
        }, "", "Apply a preset", (string val) =>
        {
            foreach (var module in _modules)
            {
                module.selectedJSON.val = false;
            }
            switch (val)
            {
                case "Native VaM possession":
                    SelectModule("HideGeometry", true);
                    SelectModule("OffsetCamera", true);
                    break;
                case "Improved possession":
                    SelectModule("Trackers", true);
                    SelectModule("HideGeometry", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
                case "Snug":
                    SelectModule("Trackers", true);
                    SelectModule("HideGeometry", true);
                    SelectModule("Snug", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
                case "Passenger":
                    SelectModule("HideGeometry", true);
                    SelectModule("Passenger", true);
                    SelectModule("WorldScale", true);
                    SelectModule("EyeTarget", true);
                    break;
            }
        });
        CreateScrollablePopup(presetsJSON, true);

        CreateSpacer(false).height = 0f;
        CreateSpacer(true).height = 6f;

        foreach (var module in _modules)
        {
            if (module.storeId == "Automation") continue;
            if (module.storeId == "Wizard") continue;
            var selectToggle = CreateToggle(module.selectedJSON, false);
            selectToggle.label = $"Select {module.label}";
            var configureButton = CreateButton($"Configure {module.label}...", true);
            configureButton.buttonText.alignment = TextAnchor.MiddleLeft;
            configureButton.buttonText.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0f);
            configureButton.buttonColor = new Color(0.8f, 0.7f, 0.8f);
            configureButton.button.onClick.AddListener(() => screensManager.Show(module.label));
            configureButton.button.interactable = module.selectedJSON.val;
            selectToggle.toggle.onValueChanged.AddListener(val => configureButton.button.interactable = val);
        }

        #warning For debugging purposes
        //context.plugin.StartCoroutine(DebugCo());
    }

    private IEnumerator DebugCo()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        // Enable Snug
        (_modules.First(m => m.storeId == "Snug") as SnugModule).showVisualCuesJSON.val = true;
        // Show 3d trackers
        (_modules.First(m => m.storeId == "Trackers") as TrackersModule).previewTrackerOffsetJSON.val = true;
        // Activate
        context.embody.activeJSON.val = true;
        // Wizard
        /*
        _modules.First(m => m.storeId == "Snug").selectedJSON.val = true;
        _modules.First(m => m.storeId == "Trackers").selectedJSON.val = false;
        _modules.First(m => m.storeId == "WorldScale").selectedJSON.val = false;
        _modules.First(m => m.storeId == "HideGeometry").selectedJSON.val = false;
        yield return new WaitForSecondsRealtime(0.2f);
        screensManager.Show(WizardScreen.ScreenName);
        yield return new WaitForSecondsRealtime(0.2f);
        _wizard.StartWizard();
        yield return new WaitForSecondsRealtime(0.2f);
        _wizard.Next();
        yield return new WaitForSecondsRealtime(0.2f);
        _wizard.Next();
        */
    }

    private void SelectModule(string storeId, bool selected)
    {
        var module = _modules.FirstOrDefault(m => m.storeId == storeId);
        if (module == null) return;
        module.selectedJSON.val = selected;
    }
}
