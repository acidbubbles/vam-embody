using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackerAutoSetup
{
    private static readonly HashSet<string> _autoBindControllers = new HashSet<string>(new[]
    {
        "chestControl",
        "lElbowControl", "rElbowControl",
        "pelvisControl",
        "lKneeControl", "rKneeControl",
        "lFootControl", "rFootControl"
    });

    private static readonly HashSet<string> _disableRotationControllers = new HashSet<string>(new[]
    {
        "lNippleControl", "rNippleControl",
        "testesControl",
        "lKneeControl", "rKneeControl",
        "lShoulderControl", "rKneeControl",
        "lElbowControl", "rElbowControl",
        "penisMidControl", "penisTipControl",
        "lToeControl", "rToeControl",
    });

    private readonly Atom _containingAtom;

    public TrackerAutoSetup(Atom containingAtom)
    {
        _containingAtom = containingAtom;
    }

    public void AttachToClosestNode(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (!motionControl.SyncMotionControl())
        {
            SuperController.LogError($"Embody: {motionControl.name} does not seem to be attached to a VR motion control.");
            return;
        }

        var position = motionControl.currentMotionControl.position;

        var closestDistance = float.PositiveInfinity;
        Rigidbody closest = null;
        foreach (var controller in _containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")).Select(fc => fc.GetComponent<Rigidbody>()))
        {
            if (!_autoBindControllers.Contains(controller.name)) continue;
            var distance = Vector3.Distance(position, controller.position);
            if (!(distance < closestDistance)) continue;
            closestDistance = distance;
            closest = controller;
        }

        if (closest == null) throw new NullReferenceException("There was no controller available to attach.");

        motionControl.mappedControllerName = closest.name;
        AlignToNode(motionControl, closest);
    }

    public void AlignToNode(MotionControllerWithCustomPossessPoint motionControl, Rigidbody rb)
    {
        if (!motionControl.SyncMotionControl())
        {
            SuperController.LogError($"Embody: {motionControl.name} does not seem to be attached to a VR motion control.");
            return;
        }

        motionControl.customOffset = motionControl.currentMotionControl.InverseTransformDirection(rb.position - motionControl.currentMotionControl.position);
        var customOffsetRotation = (Quaternion.Inverse(motionControl.currentMotionControl.rotation) * rb.rotation).eulerAngles;
        motionControl.customOffsetRotation = new Vector3(
            customOffsetRotation.x > 180 ? customOffsetRotation.x - 360 : customOffsetRotation.x,
            customOffsetRotation.y > 180 ? customOffsetRotation.y - 360 : customOffsetRotation.y,
            customOffsetRotation.z > 180 ? customOffsetRotation.z - 360 : customOffsetRotation.z
        );
        motionControl.possessPointRotation = Vector3.zero;
        motionControl.controlRotation = !_disableRotationControllers.Contains(rb.name);
    }
}
