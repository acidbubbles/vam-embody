using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Leap.Unity;
using UnityEngine;

public class TrackersSettingsScreen : ScreenBase, IScreen
{
    public const string ScreenName = TrackersModule.Label;

    private const string _none = "None";

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

        _section.CreateButton("Attach and align to closest node", true).button.onClick.AddListener(() =>
        {
            if (motionControl.mappedControllerName != null)
            {
                SuperController.LogError($"Embody: {motionControl.name} Already attached to {motionControl.mappedControllerName}");
                return;
            }

            var possessPoint = motionControl.possessPointTransform.position;

            var closestDistance = float.PositiveInfinity;
            Rigidbody closest = null;
            foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")).Select(fc => fc.GetComponent<Rigidbody>()))
            {
                var distance = Vector3.Distance(possessPoint, controller.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = controller;
                }
            }
            if (closest == null) throw new NullReferenceException("There was no controller available to attach.");

            motionControl.mappedControllerName = closest.name;
            motionControl.customOffset =  motionControl.possessPointTransform.InverseTransformDirection(closest.position - possessPoint);
            motionControl.customOffsetRotation = (Quaternion.Inverse(motionControl.possessPointTransform.rotation) * closest.rotation).eulerAngles;
            context.Refresh();
        });
    }
}
