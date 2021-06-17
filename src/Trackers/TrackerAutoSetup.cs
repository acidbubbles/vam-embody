using System;
using System.Collections;
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

    private readonly EmbodyContext _context;

    public TrackerAutoSetup(EmbodyContext context)
    {
        _context = context;
    }

    public void AttachToClosestNode(MotionControllerWithCustomPossessPoint motionControl)
    {
        var controllers = _context.containingAtom.freeControllers
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
        motionControl.rotateAroundTrackerCustom = Vector3.zero;
        motionControl.controlRotation = !_disableRotationControllers.Contains(controller.name);
        if (_centeredControllers.Contains(controller.name))
        {
            motionControl.offsetControllerCustom.Scale(new Vector3(1f, 0f, 1f));
            motionControl.rotateControllerCustom.Scale(new Vector3(1f, 0f, 0f));
        }
    }

    public void AlignAll(Action onComplete)
    {
        this._context.containingAtom.StartCoroutine(AlignAllCo(onComplete));
    }

    private IEnumerator AlignAllCo(Action onComplete)
    {
        for (var i = 5; i > 0; i--)
        {
            SuperController.singleton.helpText = $"Mapping vive controllers in {i}..";
            yield return new WaitForSecondsRealtime(1f);
        }

        _context.diagnostics.TakeSnapshot($"{nameof(TrackerAutoSetup)}.{nameof(AlignAll)}.Before");
        AlignAllNow();
        onComplete();
        _context.Refresh();
        _context.diagnostics.TakeSnapshot($"{nameof(TrackerAutoSetup)}.{nameof(AlignAll)}.After");

        SuperController.singleton.helpText = $"Mapped {_context.trackers.viveTrackers.Count(t => t.mappedControllerName != null)} vive controllers.";
        yield return new WaitForSecondsRealtime(1f);
        SuperController.singleton.helpText = "";
    }

    public string AlignAllNow()
    {
        foreach (var mc in _context.trackers.viveTrackers)
        {
            mc.ResetToDefault();
        }

        var hashSet = new HashSet<string>();
        string lastError = null;
        foreach (var mc in _context.trackers.viveTrackers)
        {
            if (!mc.SyncMotionControl()) continue;
            AttachToClosestNode(mc);
            if (!hashSet.Add(mc.mappedControllerName))
            {
                lastError = $"The tracker {mc.currentMotionControl.name} could not be bound to {mc.mappedControllerName} because that node was already bound to another tracker";
                SuperController.LogError(lastError);
                _context.diagnostics.Log(lastError);
                mc.mappedControllerName = null;
            }
        }
        return lastError;
    }
}
