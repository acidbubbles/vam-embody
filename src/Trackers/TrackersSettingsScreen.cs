using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    private readonly ITrackersModule _trackers;
    public const string ScreenName = TrackersModule.Label;
    private CollapsibleSection _section;

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

        CreateFilterablePopup(new JSONStorableStringChooser(
            "",
            _trackers.motionControls.Select(mc => mc.name).ToList(),
            _trackers.motionControls[0].name,
            "Motion control",
            (val) => ShowMotionControl(_trackers.motionControls.FirstOrDefault(mc => mc.name == val))
        ));

        ShowMotionControl(_trackers.motionControls[0]);
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (_section == null) _section = CreateSection();
        _section.RemoveAll();

        if (motionControl == null) return;

        _section.CreateFilterablePopup(new JSONStorableStringChooser(
            "",
            _trackers.controllers.Select(mc => mc.controller.name).ToList(),
            motionControl.mappedControllerName,
            "Map to control",
            val =>
            {
                motionControl.mappedControllerName = val;
                context.Refresh();
            }));

        var offsetX = new JSONStorableFloat(
            $"{motionControl.name} Offset X",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(val, motionControl.customOffset.y, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        _section.CreateSlider(offsetX);

        var offsetY = new JSONStorableFloat(
            $"{motionControl.name} Offset Y",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false);
        _section.CreateSlider(offsetY);

        var offsetZ = new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, motionControl.customOffset.y, val),
            -0.5f,
            0.5f,
            false);
        _section.CreateSlider(offsetZ);

        var offsetRotateX = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate X",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(val, motionControl.offsetRotation.y, motionControl.offsetRotation.z),
            -180,
            180,
            false);
        _section.CreateSlider(offsetRotateX);

        var offsetRotateY = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Y",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(motionControl.offsetRotation.x, val, motionControl.offsetRotation.z),
            -180,
            180,
            false);
        _section.CreateSlider(offsetRotateY);

        var offsetRotateZ = new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Z",
            motionControl.offsetRotation.x,
            val => motionControl.offsetRotation = new Vector3(motionControl.offsetRotation.x, motionControl.offsetRotation.y, val),
            -180,
            180,
            false);
        _section.CreateSlider(offsetRotateZ);

        var pointRotateX = new JSONStorableFloat(
            $"{motionControl.name} Point rotate X",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(val, motionControl.possessPointRotation.y, motionControl.possessPointRotation.z),
            -180,
            180,
            false);
        _section.CreateSlider(pointRotateX);

        var pointRotateY = new JSONStorableFloat(
            $"{motionControl.name} Point rotate Y",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, val, motionControl.possessPointRotation.z),
            -180,
            180,
            false);
        _section.CreateSlider(pointRotateY);

        var pointRotateZ = new JSONStorableFloat(
            $"{motionControl.name} Point rotate Z",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, motionControl.possessPointRotation.y, val),
            -180,
            180,
            false);
        _section.CreateSlider(pointRotateZ);
    }
}
