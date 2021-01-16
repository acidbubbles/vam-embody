using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    public const string ScreenName = TrackersModule.Label;

    private const string _none = "None";

    private readonly ITrackersModule _trackers;
    private CollapsibleSection _section;

    public TrackersSettingsScreen(EmbodyContext context, ITrackersModule trackers)
        : base(context)
    {
        _trackers = trackers;
    }

    public void Show()
    {
        if (ShowNotSelected(_trackers.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Binds VR trackers (such as the headset or controllers) to an atom's controllers.\n\nHands have no effect when Snug is enabled, and Head has no effect when Passenger is enabled."), true);

        CreateToggle(_trackers.restorePoseAfterPossessJSON, true).label = "Restore pose after possession";

        var showLineJSON = new JSONStorableBool("Preview 3D offset", _trackers.motionControls[0].showLine, (bool val) =>
        {
            foreach (var mc in _trackers.motionControls)
                mc.showLine = val;
        });
        CreateToggle(showLineJSON, true);

        CreateFilterablePopup(new JSONStorableStringChooser(
            "",
           _trackers.motionControls.Select(mc => mc.name).ToList(),
            _trackers.motionControls[0].name ?? _none,
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
            new[]{_none}.Concat(_trackers.controllers.Select(mc => mc.controller.name)).ToList(),
            motionControl.mappedControllerName,
            "Map to control",
            val =>
            {
                motionControl.mappedControllerName = val == _none ? null : val;
                context.Refresh();
            }), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset X",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(val, motionControl.customOffset.y, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset Y",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, motionControl.customOffset.y, val),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset rotate X",
            motionControl.customOffsetRotation.x,
            val => motionControl.customOffsetRotation = new Vector3(val, motionControl.customOffsetRotation.y, motionControl.customOffsetRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Y",
            motionControl.customOffsetRotation.x,
            val => motionControl.customOffsetRotation = new Vector3(motionControl.customOffsetRotation.x, val, motionControl.customOffsetRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Z",
            motionControl.customOffsetRotation.x,
            val => motionControl.customOffsetRotation = new Vector3(motionControl.customOffsetRotation.x, motionControl.customOffsetRotation.y, val),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Point rotate X",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(val, motionControl.possessPointRotation.y, motionControl.possessPointRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Point rotate Y",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, val, motionControl.possessPointRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Point rotate Z",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, motionControl.possessPointRotation.y, val),
            -180,
            180,
            false), false);
    }
}
