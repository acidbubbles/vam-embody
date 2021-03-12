using System.Collections;
using UnityEngine;

public class MainScreen : ScreenBase, IScreen
{
    private readonly IEmbodyModule[] _modules;
    public const string ScreenName = "Embody (Main)";

    public MainScreen(EmbodyContext context, IEmbodyModule[] modules)
        : base(context)
    {
        _modules = modules;
    }

    public void Show()
    {
        if (context.containingAtom.type == "Person")
        {
            CreateSpacer().height = 38f;

            var wizardBtn = CreateButton("Launch Wizard...");
            wizardBtn.button.onClick.AddListener(() => screensManager.Show(WizardScreen.ScreenName));
            if (context.worldScale.worldScaleMethodJSON.val == WorldScaleModule.EyeDistanceMethod)
                wizardBtn.buttonColor = Color.green;

            CreateSpacer().height = 37f;

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
        }
        else
        {
            CreateSpacer().height = 26f;

            CreateText(new JSONStorableString("", @"
Welcome to <b>Embody</b>! Since the plugin was applied on a non-person atom, only Passenger is available.
".Trim()), true);
        }

        if (context.containingAtom.type == "Person")
            CreateScrollablePopup(context.embody.presetsJSON, true);

        CreateSpacer(true).height = 6f;

        foreach (var module in _modules)
        {
            if (module.storeId == AutomationModule.Label) continue;
            if (module.storeId == WizardModule.Label) continue;
            if (module.storeId == DiagnosticsModule.Label) continue;
            var selectToggle = CreateToggle(module.selectedJSON, false);
            selectToggle.label = $"Select {module.label}";
            var label = module.label;
            /*var configureButton = */CreateConfigButton(label, $"Configure {label}..."/*, module.selectedJSON.val*/);
            // selectToggle.toggle.onValueChanged.AddListener(val => configureButton.button.interactable = val);
        }

        if (context.containingAtom.type == "Person")
        {
            CreateConfigButton(MoreScreen.ScreenName, "<i>More tools & options...</i>");
            CreateConfigButton(ImportExportScreen.ScreenName, $"<i>Import, Export & Default Settings...</i>");
        }
    }

    private UIDynamicButton CreateConfigButton(string screenName, string btnLabel/*, bool interactable = true*/)
    {
        var configureButton = CreateButton(btnLabel, true);
        configureButton.buttonText.alignment = TextAnchor.MiddleLeft;
        configureButton.buttonText.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0f);
        configureButton.buttonColor = new Color(0.8f, 0.7f, 0.8f);
        configureButton.button.onClick.AddListener(() => screensManager.Show(screenName));
        // configureButton.button.interactable = interactable;
        return configureButton;
    }
}
