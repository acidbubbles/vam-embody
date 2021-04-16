using System.Collections.Generic;
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
    private MotionControllerWithCustomPossessPoint _currentMotionControl;

    public TrackersSettingsScreen(EmbodyContext context, ITrackersModule trackers)
        : base(context)
    {
        _trackers = trackers;
        _trackerAutoSetup = new TrackerAutoSetup(context);
    }

    public void Show()
    {
        CreateText(new JSONStorableString("", "Binds VR trackers (such as the headset or controllers) to an atom's controllers.\n\nHands have no effect when Snug is enabled, and Head has no effect when Passenger is enabled."), true);

        CreateToggle(_trackers.importDefaultsOnLoad, true).label = "* Use Defaults (Don't Save)";
        CreateToggle(_trackers.restorePoseAfterPossessJSON, true).label = "Restore pose after possession";
        CreateToggle(_trackers.previewTrackerOffsetJSON, true).label = "Preview offset in 3D";

        _motionControlJSON = _motionControlJSON ?? new JSONStorableStringChooser(
            "",
            new List<string>(),
            _trackers.motionControls[0].name ?? _none,
            "Tracker",
            (val) => ShowMotionControl(_trackers.motionControls.FirstOrDefault(mc => mc.name == val))
        );
        CreateScrollablePopup(_motionControlJSON);
        _motionControlJSON.choices = _trackers.motionControls
            .Where(mc => mc.SyncMotionControl() || MotionControlNames.IsHeadOrHands(mc.name))
            .Select(mc => mc.name)
            .ToList();

        ShowMotionControl(_trackers.motionControls.FirstOrDefault(mc => mc.name == _motionControlJSON.val));
    }

    public override void Hide()
    {
        base.Hide();
        _section = null;
        if (_currentMotionControl != null)
            _currentMotionControl.highlighted = false;
        _currentMotionControl = null;
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void ShowMotionControl(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (_section == null) _section = CreateSection();
        _section.RemoveAll();

        if (_currentMotionControl != null)
            _currentMotionControl.highlighted = false;

        if (motionControl == null) return;

        _currentMotionControl = motionControl;
        _currentMotionControl.highlighted = true;

        if (MotionControlNames.IsViveTracker(motionControl.name))
        {
            _section.CreateButton("Map to Closest Control", true).button.onClick.AddListener(() =>
            {
                _trackerAutoSetup.AttachToClosestNode(motionControl);
                ShowMotionControl(motionControl);
                context.Refresh();
            });

            _section.CreateButton("Align to Mapped Control", true).button.onClick.AddListener(() =>
            {
                var controller = context.containingAtom.freeControllers.FirstOrDefault(fc => fc.name == motionControl.mappedControllerName);
                if (controller != null)
                {
                    _trackerAutoSetup.AlignToNode(motionControl, controller);
                    ShowMotionControl(motionControl);
                    context.Refresh();
                }
            });
        }

        if (context.trackers.viveTrackers.Any())
        {
            _section.CreateButton("Align <i>All</i> Vive Trackers...", true).button.onClick.AddListener(() =>
            {
                _trackerAutoSetup.AlignAll(
                    () =>
                    {
                        if (_currentMotionControl == null || !MotionControlNames.IsViveTracker(_currentMotionControl.name)) return;
                        ShowMotionControl(_currentMotionControl);
                        if (context.embody.activeJSON.val)
                            context.Refresh();
                        else
                            context.embody.activeJSON.val = true;
                    });
            });
        }

        _section.CreateToggle(new JSONStorableBool(
            $"{motionControl.name} Enabled",
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
                "Map to control*",
                val =>
                {
                    motionControl.mappedControllerName = val == _none ? null : val;
                    context.Refresh();
                }), false);

            _section.CreateToggle(new JSONStorableBool(
                $"{motionControl.name} Controls Position*",
                motionControl.controlPosition,
                val => motionControl.controlPosition = val
            ), false);

            _section.CreateToggle(new JSONStorableBool(
                $"{motionControl.name} Controls Rotation*",
                motionControl.controlRotation,
                val => motionControl.controlRotation = val
            ), false);
        }

        if (MotionControlNames.IsHands(motionControl.name))
        {
            _section.CreateToggle(new JSONStorableBool(
                $"Fingers Tracking*",
                motionControl.fingersTracking,
                val => motionControl.fingersTracking = val
            ), false);

            _section.CreateToggle(new JSONStorableBool(
                $"Position Tracking*",
                motionControl.controlPosition,
                val => motionControl.controlPosition = motionControl.controlRotation = val
            ), false);

            _section.CreateToggle(new JSONStorableBool(
                $"Use Leap Motion Position*",
                motionControl.useLeapPositioning,
                val => motionControl.useLeapPositioning = val
            ), false);
        }

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset X*",
            motionControl.offsetControllerCustom.x,
            val => motionControl.offsetControllerCustom = new Vector3(val, motionControl.offsetControllerCustom.y, motionControl.offsetControllerCustom.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset Y*",
            motionControl.offsetControllerCustom.y,
            val => motionControl.offsetControllerCustom = new Vector3(motionControl.offsetControllerCustom.x, val, motionControl.offsetControllerCustom.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Offset Z*",
            motionControl.offsetControllerCustom.z,
            val => motionControl.offsetControllerCustom = new Vector3(motionControl.offsetControllerCustom.x, motionControl.offsetControllerCustom.y, val),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller X*",
            motionControl.rotateControllerCustom.x,
            val => motionControl.rotateControllerCustom = new Vector3(val, motionControl.rotateControllerCustom.y, motionControl.rotateControllerCustom.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller Y*",
            motionControl.rotateControllerCustom.y,
            val => motionControl.rotateControllerCustom = new Vector3(motionControl.rotateControllerCustom.x, val, motionControl.rotateControllerCustom.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Controller Z*",
            motionControl.rotateControllerCustom.z,
            val => motionControl.rotateControllerCustom = new Vector3(motionControl.rotateControllerCustom.x, motionControl.rotateControllerCustom.y, val),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker X*",
            motionControl.rotateAroundTrackerCustom.x,
            val => motionControl.rotateAroundTrackerCustom = new Vector3(val, motionControl.rotateAroundTrackerCustom.y, motionControl.rotateAroundTrackerCustom.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker Y*",
            motionControl.rotateAroundTrackerCustom.y,
            val => motionControl.rotateAroundTrackerCustom = new Vector3(motionControl.rotateAroundTrackerCustom.x, val, motionControl.rotateAroundTrackerCustom.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"Rotate Around Tracker Z*",
            motionControl.rotateAroundTrackerCustom.z,
            val => motionControl.rotateAroundTrackerCustom = new Vector3(motionControl.rotateAroundTrackerCustom.x, motionControl.rotateAroundTrackerCustom.y, val),
            -180,
            180,
            false), false);

        _section.CreateToggle(new JSONStorableBool(
            "Keep current physics hold strength*",
            motionControl.keepCurrentPhysicsHoldStrength,
            val => motionControl.keepCurrentPhysicsHoldStrength = val), false);

        if (MotionControlNames.IsHands(motionControl.name))
        {
            _section.CreateButton("Sync to other hand").button.onClick.AddListener(() =>
            {
                var otherHand = motionControl.name == MotionControlNames.LeftHand ? _trackers.rightHandMotionControl : _trackers.leftHandMotionControl;

                otherHand.offsetControllerCustom = new Vector3(-motionControl.offsetControllerCustom.x, motionControl.offsetControllerCustom.y, motionControl.offsetControllerCustom.z);
                otherHand.rotateControllerCustom = new Vector3(motionControl.rotateControllerCustom.x, -motionControl.rotateControllerCustom.y, -motionControl.rotateControllerCustom.z);
                otherHand.rotateAroundTrackerCustom = new Vector3(motionControl.rotateAroundTrackerCustom.x, -motionControl.rotateAroundTrackerCustom.y, -motionControl.rotateAroundTrackerCustom.z);
            });
        }
    }
}
