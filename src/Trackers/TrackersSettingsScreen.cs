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
        foreach (var customizedMotionControl in _trackers.customizedMotionControls)
        {
            ShowMotionControl(customizedMotionControl);
        }
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        var possessPoint = motionControl.customPossessPoint;

        var offsetX = new JSONStorableFloat(
            $"{motionControl.name} Offset X",
            possessPoint.localPosition.x,
            (float val) => possessPoint.localPosition = new Vector3(val, possessPoint.localPosition.y, possessPoint.localPosition.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetX);

        var offsetY = new JSONStorableFloat(
            $"{motionControl.name} Offset Y",
            possessPoint.localPosition.x,
            (float val) => possessPoint.localPosition = new Vector3(possessPoint.localPosition.x, val, possessPoint.localPosition.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetY);

        var offsetZ = new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            possessPoint.localPosition.x,
            (float val) => possessPoint.localPosition = new Vector3(possessPoint.localPosition.x, possessPoint.localPosition.y, val),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetZ);

        var rotateX = new JSONStorableFloat(
            $"{motionControl.name} Rotate X",
            possessPoint.localEulerAngles.x,
            (float val) => possessPoint.localEulerAngles = new Vector3(val, possessPoint.localEulerAngles.y, possessPoint.localEulerAngles.z),
            -180,
            180,
            false);
        CreateSlider(rotateX);

        var rotateY = new JSONStorableFloat(
            $"{motionControl.name} Rotate Y",
            possessPoint.localEulerAngles.x,
            (float val) => possessPoint.localEulerAngles = new Vector3(possessPoint.localEulerAngles.x, val, possessPoint.localEulerAngles.z),
            -180,
            180,
            false);
        CreateSlider(rotateY);

        var rotateZ = new JSONStorableFloat(
            $"{motionControl.name} Rotate Z",
            possessPoint.localEulerAngles.x,
            (float val) => possessPoint.localEulerAngles = new Vector3(possessPoint.localEulerAngles.x, possessPoint.localEulerAngles.y, val),
            -180,
            180,
            false);
        CreateSlider(rotateZ);
    }
}
