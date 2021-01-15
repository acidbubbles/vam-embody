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
            #warning Temporary
            if (customizedMotionControl.name != "LeftHand") continue;
            ShowMotionControl(customizedMotionControl);
        }
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        var offsetX = new JSONStorableFloat(
            $"{motionControl.name} Offset X",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(val, motionControl.customOffset.y, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetX);

        var offsetY = new JSONStorableFloat(
            $"{motionControl.name} Offset Y",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetY);

        var offsetZ = new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, motionControl.customOffset.y, val),
            -0.5f,
            0.5f,
            false);
        CreateSlider(offsetZ);

        var offsetRotateX = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate X",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(val, motionControl.offsetRotation.y, motionControl.offsetRotation.z),
            -180,
            180,
            false);
        CreateSlider(offsetRotateX);

        var offsetRotateY = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Y",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(motionControl.offsetRotation.x, val, motionControl.offsetRotation.z),
            -180,
            180,
            false);
        CreateSlider(offsetRotateY);

        var offsetRotateZ = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Z",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(motionControl.offsetRotation.x, motionControl.offsetRotation.y, val),
            -180,
            180,
            false);
        CreateSlider(offsetRotateZ);

        var pointRotateX = new JSONStorableFloat(
            $"{motionControl.name} Point rotate X",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(val, motionControl.possessPointRotation.y, motionControl.possessPointRotation.z),
            -180,
            180,
            false);
        CreateSlider(pointRotateX);

        var pointRotateY = new JSONStorableFloat(
            $"{motionControl.name} Point rotate Y",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, val, motionControl.possessPointRotation.z),
            -180,
            180,
            false);
        CreateSlider(pointRotateY);

        var pointRotateZ = new JSONStorableFloat(
            $"{motionControl.name} Point rotate Z",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, motionControl.possessPointRotation.y, val),
            -180,
            180,
            false);
        CreateSlider(pointRotateZ);
    }
}
