using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    public const string ScreenName = TrackersModule.Label;

    private const string _none = "None";

    private readonly ITrackersModule _trackers;
    private CollapsibleSection _section;
    private JSONStorableStringChooser _motionControlJSON;
    private readonly TrackerAutoSetup _trackerAutoSetup;

    public TrackersSettingsScreen(EmbodyContext context, ITrackersModule trackers)
        : base(context)
    {
        _trackers = trackers;
        _trackerAutoSetup = new TrackerAutoSetup(context.containingAtom);
    }

    public void Show()
    {
        if (ShowNotSelected(_trackers.selectedJSON.val)) return;

        CreateText(new JSONStorableString("", "Binds VR trackers (such as the headset or controllers) to an atom's controllers.\n\nHands have no effect when Snug is enabled, and Head has no effect when Passenger is enabled."), true);

        CreateToggle(_trackers.restorePoseAfterPossessJSON, true).label = "Restore pose after possession";

        CreateToggle(_trackers.previewTrackerOffsetJSON, true).label = "Preview offset in 3D";

        _motionControlJSON = _motionControlJSON ?? new JSONStorableStringChooser(
            "",
            _trackers.motionControls.Select(mc => mc.name).ToList(),
            _trackers.motionControls[0].name ?? _none,
            "Tracker",
            (val) => ShowMotionControl(_trackers.motionControls.FirstOrDefault(mc => mc.name == val))
        );
        CreateScrollablePopup(_motionControlJSON);

        ShowMotionControl(_trackers.motionControls[0]);
    }

    public override void Hide()
    {
        base.Hide();
        _section = null;
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (_section == null) _section = CreateSection();
        _section.RemoveAll();

        if (motionControl == null) return;

        if (!MotionControlNames.IsHeadOrHands(motionControl.name))
        {
            _section.CreateButton("Map to Closest Control", true).button.onClick.AddListener(() =>
            {
                _trackerAutoSetup.AttachToClosestNode(motionControl);
                ShowMotionControl(motionControl);
                context.Refresh();
            });

            _section.CreateButton("Align to Mapped Control", true).button.onClick.AddListener(() =>
            {
                _trackerAutoSetup.AlignToNode(motionControl, context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == motionControl.mappedControllerName)?.GetComponent<Rigidbody>());
                ShowMotionControl(motionControl);
                context.Refresh();
            });
        }

        _section.CreateToggle(new JSONStorableBool(
            $" Enabled",
            motionControl.enabled,
            val =>
            {
                motionControl.enabled = val;
                context.Refresh();
            }
        ));

        if (!MotionControlNames.IsHeadOrHands(motionControl.name))
        {
            _section.CreateFilterablePopup(new JSONStorableStringChooser(
                "",
                new[] {_none}.Concat(_trackers.controllers.Select(mc => mc.controller.name)).ToList(),
                motionControl.mappedControllerName,
                "Map to control",
                val =>
                {
                    motionControl.mappedControllerName = val == _none ? null : val;
                    context.Refresh();
                }), false);

            _section.CreateToggle(new JSONStorableBool(
                $"Control Rotation",
                motionControl.controlRotation,
                val => motionControl.controlRotation = val
            ), false);
        }

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset X",
            motionControl.customOffset.x,
            val => motionControl.customOffset = new Vector3(val, motionControl.customOffset.y, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset Y",
            motionControl.customOffset.y,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset Z",
            motionControl.customOffset.z,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, motionControl.customOffset.y, val),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller X",
            motionControl.customOffsetRotation.x,
            val => motionControl.customOffsetRotation = new Vector3(val, motionControl.customOffsetRotation.y, motionControl.customOffsetRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller Y",
            motionControl.customOffsetRotation.y,
            val => motionControl.customOffsetRotation = new Vector3(motionControl.customOffsetRotation.x, val, motionControl.customOffsetRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller Z",
            motionControl.customOffsetRotation.z,
            val => motionControl.customOffsetRotation = new Vector3(motionControl.customOffsetRotation.x, motionControl.customOffsetRotation.y, val),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker X",
            motionControl.possessPointRotation.x,
            val => motionControl.possessPointRotation = new Vector3(val, motionControl.possessPointRotation.y, motionControl.possessPointRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker Y",
            motionControl.possessPointRotation.y,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, val, motionControl.possessPointRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker Z",
            motionControl.possessPointRotation.z,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, motionControl.possessPointRotation.y, val),
            -180,
            180,
            false), false);
    }
}
