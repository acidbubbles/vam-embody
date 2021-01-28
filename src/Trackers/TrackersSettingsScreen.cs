using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    public const string ScreenName = TrackersModule.Label;

    private const string _none = "None";

    private static HashSet<string> _skipAutoBindControllers = new HashSet<string>(new[]
    {
        "lNippleControl", "rNippleControl",
        "eyeTargetControl",
        "testesControl",
        "penisMidControl", "penisTipControl",
        "lToeControl", "rToeControl",
    });

    private readonly ITrackersModule _trackers;
    private CollapsibleSection _section;
    private JSONStorableStringChooser _motionControlJSON;

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

        CreateToggle(_trackers.previewTrackerOffsetJSON, true).label = "Preview offset in 3D";

        _motionControlJSON = _motionControlJSON ?? new JSONStorableStringChooser(
            "",
            _trackers.motionControls.Select(mc => mc.name).ToList(),
            _trackers.motionControls[0].name ?? _none,
            "Motion control",
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

        _section.CreateToggle(new JSONStorableBool(
            $"{motionControl.name}  Enabled",
            motionControl.enabled,
            val =>
            {
                motionControl.enabled = val;
                context.Refresh();
            }
        ));

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
            motionControl.customOffset.y,
            val => motionControl.customOffset = new Vector3(motionControl.customOffset.x, val, motionControl.customOffset.z),
            -0.5f,
            0.5f,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset Z",
            motionControl.customOffset.z,
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
            motionControl.customOffsetRotation.y,
            val => motionControl.customOffsetRotation = new Vector3(motionControl.customOffsetRotation.x, val, motionControl.customOffsetRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Offset rotate Z",
            motionControl.customOffsetRotation.z,
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
            motionControl.possessPointRotation.y,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, val, motionControl.possessPointRotation.z),
            -180,
            180,
            false), false);

        _section.CreateSlider(new JSONStorableFloat(
            $"{motionControl.name} Point rotate Z",
            motionControl.possessPointRotation.z,
            val => motionControl.possessPointRotation = new Vector3(motionControl.possessPointRotation.x, motionControl.possessPointRotation.y, val),
            -180,
            180,
            false), false);

        _section.CreateButton("Attach and align to closest node", true).button.onClick.AddListener(() => { AttachToClosestNode(motionControl); });
    }

    private void AttachToClosestNode(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (!motionControl.SyncMotionControl())
        {
            SuperController.LogError($"Embody: {motionControl.name} does not seem to be attached to a VR motion control.");
            return;
        }

        var position = motionControl.currentMotionControl.position;

        var closestDistance = float.PositiveInfinity;
        Rigidbody closest = null;
        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")).Select(fc => fc.GetComponent<Rigidbody>()))
        {
            if (_skipAutoBindControllers.Contains(controller.name)) continue;
            var distance = Vector3.Distance(position, controller.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = controller;
            }
        }

        if (closest == null) throw new NullReferenceException("There was no controller available to attach.");

        motionControl.mappedControllerName = closest.name;
        motionControl.customOffset = motionControl.possessPointTransform.InverseTransformDirection(closest.position - position);
        motionControl.customOffsetRotation = (Quaternion.Inverse(motionControl.possessPointTransform.rotation) * closest.rotation).eulerAngles;
        ShowMotionControl(motionControl);
        context.Refresh();
    }
}
