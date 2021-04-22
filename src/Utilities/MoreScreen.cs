using System;
using System.Collections.Generic;
using UnityEngine;

public class MoreScreen : ScreenBase, IScreen
{
    public const string ScreenName = "More";

    public MoreScreen(EmbodyContext context)
        : base(context)
    {
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Additional tools and options can be found here.\n\nOptions marked with an asterisk will only be saved in your profile."), true);

        CreateSpacer().height = 20f;
        CreateTitle("Create Atoms");
        CreateButton("Create Mirror").button.onClick.AddListener(() => SuperController.singleton.StartCoroutine(Utilities.CreateMirror(context.eyeTarget, context.containingAtom)));

        CreateSpacer().height = 20f;
        CreateTitle("Animation");
        CreateToggle(context.automation.autoArmForRecord).label = "Auto Arm On Active*";
        CreateButton("Arm Possessed Controllers & Record").button.onClick.AddListener(() => Utilities.StartRecord(context));

        CreateSpacer().height = 20f;
        CreateTitle("Posing");
        CreateButton("Apply Possession-Ready Pose").button.onClick.AddListener(() =>
        {
            var pose = new PossessionPose(context);
            pose.Apply();
        });
        CreateButton("Re-Center Pose Near Root").button.onClick.AddListener(() => Utilities.ReCenterPose(context.containingAtom));
        CreateButton("Disable Untracked Controls").button.onClick.AddListener(() => Utilities.DisableUntrackedControls(context));

        CreateTitle("General Settings", true);
#if(VAM_GT_1_20_77_0)
        CreateToggle(context.automation.takeOverVamPossess, true).label = "Take Over Virt-A-Mate Possession*";
#endif
        var toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", GetKeys(), KeyCode.None.ToString(), "Toggle Key*",
            val => { context.automation.toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        var toggleKeyPopup = CreateFilterablePopup(toggleKeyJSON, true);
        toggleKeyPopup.popupPanelHeight = 700f;

        CreateSpacer(true).height = 20f;
        CreateTitle("Browser Links", true);
        var helpButton = CreateButton("[Browser] Online Help", true);
        helpButton.button.onClick.AddListener(() => Application.OpenURL("https://github.com/acidbubbles/vam-embody/wiki"));

        var patreonBtn = CreateButton("[Browser] Support Me On Patreon ♥", true);
        patreonBtn.textColor = new Color(0.97647f, 0.40784f, 0.32941f);
        patreonBtn.buttonColor = Color.white;
        patreonBtn.button.onClick.AddListener(() => Application.OpenURL("https://www.patreon.com/acidbubbles"));

        CreateSpacer(true).height = 20f;
        CreateTitle("Advanced", true);
        CreateToggle(context.wizard.experimentalSnugWizardJSON).label = "Experimental: Snug Wizard";
        CreateButton("<i>Diagnostics...</i>", true).button.onClick.AddListener(() =>
        {
            screensManager.Show(DiagnosticsScreen.ScreenName);
        });
    }

    private static List<string> _keys;
    private static List<string> GetKeys()
    {
        if (_keys != null) return _keys;

        _keys = Enum.GetNames(typeof(KeyCode)).ToList();
        _keys.Remove(KeyCode.Mouse0.ToString());
        _keys.Remove(KeyCode.Escape.ToString());
        _keys.Remove(KeyCode.None.ToString());
        _keys.Insert(0, KeyCode.None.ToString());
        return _keys;
    }
}
