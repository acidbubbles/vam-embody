using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        CreateText(new JSONStorableString("", "Additional tools and options can be found here."), true);

        CreateButton("Create Mirror").button.onClick.AddListener(CreateMirror);
        CreateButton("Arm Possessed Controllers & Record").button.onClick.AddListener(StartRecord);
        CreateButton("Apply Possession-Ready Pose").button.onClick.AddListener(ResetPose);
        CreateButton("Reset All Defaults").button.onClick.AddListener(() => Utilities.ResetToDefaults(context));

        CreateToggle(context.automation.takeOverVamPossess, true).label = "Take Over Virt-A-Mate Possession";

        var toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", GetKeys(), KeyCode.None.ToString(), "Toggle Key",
            val => { context.automation.toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        var toggleKeyPopup = CreateFilterablePopup(toggleKeyJSON, true);
        toggleKeyPopup.popupPanelHeight = 700f;
    }

    private void CreateMirror()
    {
        SuperController.singleton.StartCoroutine(Utilities.CreateMirror(context.eyeTarget, context.containingAtom));
    }

    private void StartRecord()
    {
        context.embody.activeJSON.val = true;
        SuperController.singleton.motionAnimationMaster.StopPlayback();
        SuperController.singleton.motionAnimationMaster.ResetAnimation();
        foreach (var controller in context.plugin.containingAtom.freeControllers.Where(fc => fc.possessed))
        {
            var mac = controller.GetComponent<MotionAnimationControl>();
            mac.ClearAnimation();
            mac.armedForRecord = true;
        }
        SuperController.singleton.SelectModeAnimationRecord();
        SuperController.singleton.StartCoroutine(WaitForRecordComplete());
    }

    private IEnumerator WaitForRecordComplete()
    {
        while (!string.IsNullOrEmpty(SuperController.singleton.helpText))
            yield return 0;
        SuperController.singleton.motionAnimationMaster.StopPlayback();
        SuperController.singleton.motionAnimationMaster.ResetAnimation();
    }

    private void ResetPose()
    {
        var useViveTrackers = context.trackers.viveTrackers.Any(t => t.SyncMotionControl());
        var step = new ResetPoseStep(context, !useViveTrackers);
        step.Apply();
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
