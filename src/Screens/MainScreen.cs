using System.Collections.Generic;
using UnityEngine;

public class MainScreen : ScreenBase, IScreen
{
    private readonly List<IEmbodyModule> _modules;
    public const string ScreenName = "Embody (Main)";

    public MainScreen(EmbodyContext context, List<IEmbodyModule> modules)
        : base(context)
    {
        _modules = modules;
    }

    public void Show()
    {
        if (context.containingAtom.type == "Person")
        {
            CreateSpacer().height = 21f;

            var wizardBtn = CreateButton("Launch Wizard...");
            wizardBtn.button.onClick.AddListener(() => screensManager.Show(WizardScreen.ScreenName));
            if (context.worldScale.worldScaleMethodJSON.val == WorldScaleModule.EyeDistanceMethod)
                wizardBtn.buttonColor = Color.green;
            CreateButton("Create Mirror").button.onClick.AddListener(() => context.plugin.StartCoroutine(Utilities.CreateMirrorCo(context.eyeTarget, context.containingAtom)));
            CreateButton("Apply Possession-Ready Pose").button.onClick.AddListener(() => new PossessionPose(context).Apply());

            CreateSpacer().height = 84;

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

<b>About profiles</b>

Whenever you add this plugin, you default profile will be loaded. Options with an asterisk* will be overwritten by what you saved in your profile and not with the scene, so make sure to save your default profile if you make changes to them! For example if you record your height, every scene you load will use your real height, so the world scale will feel right without having to manually tweak scenes you download.
".Trim()), true);
        }
        else
        {
            CreateSpacer().height = 50f;

            CreateText(new JSONStorableString("", @"
Welcome to <b>Embody</b>! Since the plugin was applied on a non-person atom, only Passenger is available.
".Trim()), true);
        }

        if (context.containingAtom.type == "Person")
        {
            CreateButton("Manage Presets...", true).button.onClick.AddListener(() => screensManager.Show(ProfilesScreen.ScreenName));
            CreateButton("Save As Default Profile", true).button.onClick.AddListener(() => new Storage(context).MakeDefault());
            CreateFilterablePopup(context.embody.presetsJSON, true);
        }

        CreateSpacer(true).height = 15f;

        foreach (var module in _modules)
        {
            if (module.storeId == AutomationModule.Label) continue;
            if (module.storeId == WizardModule.Label) continue;
            if (module.storeId == DiagnosticsModule.Label) continue;
            var selectToggle = CreateToggle(module.selectedJSON, false);
            selectToggle.label = $"Select {module.label}";
            var label = module.label;
            CreateConfigButton(label, $"Configure {label}...");
        }

        CreateConfigButton(MoreScreen.ScreenName, "<i>Other Settings...</i>");

        var triggersBtn = CreateButton("Configure Triggers");
        triggersBtn.buttonText.alignment = TextAnchor.MiddleLeft;
        triggersBtn.buttonText.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0f);
        triggersBtn.button.onClick.AddListener(context.embody.activateTrigger.trigger.OpenTriggerActionsPanel);
    }

    private void CreateConfigButton(string screenName, string btnLabel)
    {
        var configureButton = CreateButton(btnLabel, true);
        configureButton.buttonText.alignment = TextAnchor.MiddleLeft;
        configureButton.buttonText.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0f);
        configureButton.buttonColor = new Color(0.8f, 0.7f, 0.8f);
        configureButton.button.onClick.AddListener(() => screensManager.Show(screenName));
    }
}
