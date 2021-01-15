using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    private readonly ITrackersModule _trackers;
    public const string ScreenName = TrackersModule.Label;

    public TrackersSettingsScreen(EmbodyContext context, ITrackersModule trackers)
        : base(context)
    {
        _trackers = trackers;
    }

    public void Show()
    {
        if (ShowNotSelected(_trackers.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Binds VR trackers (such as the headset or controllers) to an atom's controllers."), true);

        CreateToggle(_trackers.restorePoseAfterPossessJSON, true).label = "Restore pose after possession";
        // TODO: Bind controllers to a specific tracker, hands, head and vive tracker 1..8

        // Prototype:
        foreach (var customizedMotionControl in _trackers.motionControls)
        {
            ShowMotionControl(customizedMotionControl);
        }
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        var offsetX = new JSONStorableFloat(
            $"{motionControl.name} Offset X",
            motionControl.customOffset.x,
            (float val) => motionControl.customOffset = new Vector3(val, motionControl.customOffset.y, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetX);

        var offsetY = new JSONStorableFloat(
            $"{motionControl.name} Offset Y",
            motionControl.customOffset.x,
            (float val) => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetY);

        var offsetZ = new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            motionControl.customOffset.x,
            (float val) => motionControl.customOffset = new Vector3(motionControl.customOffset.x, motionControl.customOffset.y, val),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetZ);

        var rotateX = new JSONStorableFloat(
            $"{motionControl.name} Rotate X",
            motionControl.localEulerAngles.x,
            (float val) => motionControl.localEulerAngles = new Vector3(val, motionControl.localEulerAngles.y, motionControl.localEulerAngles.z),
            -180,
            180,
            false);
        CreateSlider(rotateX);

        var rotateY = new JSONStorableFloat(
            $"{motionControl.name} Rotate Y",
            motionControl.localEulerAngles.x,
            (float val) => motionControl.localEulerAngles = new Vector3(motionControl.localEulerAngles.x, val, motionControl.localEulerAngles.z),
            -180,
            180,
            false);
        CreateSlider(rotateY);

        var rotateZ = new JSONStorableFloat(
            $"{motionControl.name} Rotate Z",
            motionControl.localEulerAngles.x,
            (float val) => motionControl.localEulerAngles = new Vector3(motionControl.localEulerAngles.x, motionControl.localEulerAngles.y, val),
            -180,
            180,
            false);
        CreateSlider(rotateZ);
    }
}
