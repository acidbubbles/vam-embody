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
        "hipControl",
        "lKneeControl", "rKneeControl",
        "lFootControl", "rFootControl"
    });

    private static readonly HashSet<string> _centeredControllers = new HashSet<string>(new[]
    {
        "chestControl",
        "hipControl",
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
        var controllers = _containingAtom.freeControllers
            .Where(fc => fc.name.EndsWith("Control"))
            .Where(fc => fc.control != null);
        AttachToClosestNode(motionControl, controllers);
    }

    public void AttachToClosestNode(MotionControllerWithCustomPossessPoint motionControl, IEnumerable<FreeControllerV3> controllers)
    {
        if (!motionControl.SyncMotionControl())
        {
            SuperController.LogError($"Embody: {motionControl.name} does not seem to be attached to a VR motion control.");
            return;
        }

        var position = motionControl.currentMotionControl.position;

        var closestDistance = float.PositiveInfinity;
        FreeControllerV3 closest = null;
        foreach (var controller in controllers)
        {
            var rigidbody = controller.GetComponent<Rigidbody>();
            if (!_autoBindControllers.Contains(controller.name)) continue;
            var distance = Vector3.Distance(position, rigidbody.position);
            if (!(distance < closestDistance)) continue;
            closestDistance = distance;
            closest = controller;
        }

        if (closest == null) throw new NullReferenceException("There was no controller available to attach.");

        motionControl.mappedControllerName = closest.name;
        AlignToNode(motionControl, closest);
    }

    public void AlignToNode(MotionControllerWithCustomPossessPoint motionControl, FreeControllerV3 controller)
    {
        if (!motionControl.SyncMotionControl())
        {
            SuperController.LogError($"Embody: {motionControl.name} does not seem to be attached to a VR motion control.");
            return;
        }

        motionControl.offsetControllerCustom = motionControl.currentMotionControl.InverseTransformPoint(controller.control.position);
        var customOffsetRotation = (Quaternion.Inverse(motionControl.currentMotionControl.rotation) * controller.control.rotation).eulerAngles;
        motionControl.rotateControllerCustom = new Vector3(
            customOffsetRotation.x > 180 ? customOffsetRotation.x - 360 : customOffsetRotation.x,
            customOffsetRotation.y > 180 ? customOffsetRotation.y - 360 : customOffsetRotation.y,
            customOffsetRotation.z > 180 ? customOffsetRotation.z - 360 : customOffsetRotation.z
        );
        motionControl.rotateAroundTracker = Vector3.zero;
        motionControl.controlRotation = !_disableRotationControllers.Contains(controller.name);
        if (_centeredControllers.Contains(controller.name))
        {
            motionControl.offsetControllerCustom.Scale(new Vector3(1f, 0f, 1f));
            motionControl.rotateControllerCustom.Scale(new Vector3(1f, 0f, 0f));
        }
    }
}
