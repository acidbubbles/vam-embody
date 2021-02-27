using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    JSONStorableBool previewTrackerOffsetJSON { get; }
    List<MotionControllerWithCustomPossessPoint> motionControls { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> viveTrackers { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> headAndHands { get; }
    MotionControllerWithCustomPossessPoint headMotionControl { get; }
    MotionControllerWithCustomPossessPoint leftHandMotionControl { get; }
    MotionControllerWithCustomPossessPoint rightHandMotionControl { get; }
    List<FreeControllerV3WithSnapshot> controllers { get; }
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public List<MotionControllerWithCustomPossessPoint> motionControls { get; } = new List<MotionControllerWithCustomPossessPoint>();
    public MotionControllerWithCustomPossessPoint headMotionControl { get; private set; }
    public MotionControllerWithCustomPossessPoint leftHandMotionControl { get; private set; }
    public MotionControllerWithCustomPossessPoint rightHandMotionControl { get; private set; }

    public IEnumerable<MotionControllerWithCustomPossessPoint> viveTrackers => motionControls
        .Where(t => MotionControlNames.IsViveTracker(t.name));

    public IEnumerable<MotionControllerWithCustomPossessPoint> headAndHands => motionControls
        .Where(t => MotionControlNames.IsHeadOrHands(t.name));

    public List<FreeControllerV3WithSnapshot> controllers { get; } = new List<FreeControllerV3WithSnapshot>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    public JSONStorableBool previewTrackerOffsetJSON { get; } = new JSONStorableBool("PreviewTrackerOffset", false);

    private FreeControllerV3WithSnapshot _leftHandController;
    private FreeControllerV3WithSnapshot _rightHandController;
    private NavigationRigSnapshot _navigationRigSnapshot;
    private Coroutine _waitForHandsCo;

    public override void Awake()
    {
        base.Awake();

        headMotionControl = AddMotionControl(MotionControlNames.Head, () => context.head, "headControl");
        leftHandMotionControl = AddMotionControl(MotionControlNames.LeftHand, () => context.LeftHand(leftHandMotionControl?.useLeapPositioning ?? false), "lHandControl", mc => HandsAdjustments.ConfigureHand(mc, false));
        rightHandMotionControl = AddMotionControl(MotionControlNames.RightHand, () => context.RightHand(rightHandMotionControl?.useLeapPositioning ?? false), "rHandControl", mc => HandsAdjustments.ConfigureHand(mc, true));
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}1", () => context.viveTracker1);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}2", () => context.viveTracker2);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}3", () => context.viveTracker3);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}4", () => context.viveTracker4);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}5", () => context.viveTracker5);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}6", () => context.viveTracker6);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}7", () => context.viveTracker7);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}8", () => context.viveTracker8);

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            var snapshot = new FreeControllerV3WithSnapshot(controller);
            controllers.Add(snapshot);
            if (controller.name == "lHandControl") _leftHandController = snapshot;
            if (controller.name == "rHandControl") _rightHandController = snapshot;
        }

        previewTrackerOffsetJSON.setCallbackFunction = val =>
        {
            foreach (var mc in motionControls)
            {
                mc.SyncMotionControl();
                mc.showPreview = val;
            }
        };
    }

    private MotionControllerWithCustomPossessPoint AddMotionControl(string motionControlName, Func<Transform> getMotionControl, string mappedControllerName = null, Action<MotionControllerWithCustomPossessPoint> configure = null)
    {
        var motionControl = MotionControllerWithCustomPossessPoint.Create(motionControlName, getMotionControl, configure);
        motionControl.mappedControllerName = mappedControllerName;
        motionControls.Add(motionControl);
        motionControl.SyncMotionControl();
        return motionControl;
    }

    public override bool BeforeEnable()
    {
        if (headMotionControl.enabled && !context.passenger.selectedJSON.val)
        {
            _navigationRigSnapshot = NavigationRigSnapshot.Snap();
            AdjustHeadToEyesOffset();
        }
        return true;
    }

    private void AdjustHeadToEyesOffset()
    {
        var motionControl = motionControls.First(mc => mc.name == MotionControlNames.Head);
        var controller = FindController(motionControl)?.controller;
        if (controller == null) return;

        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var eyesCenter = (eyes.First(eye => eye.name == "lEye").transform.localPosition + eyes.First(eye => eye.name == "rEye").transform.localPosition) / 2f;
        motionControl.offsetControllerBase = -eyesCenter;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        Bind(headMotionControl);
        var leftHandBound = Bind(leftHandMotionControl);
        var rightHandBound = Bind(rightHandMotionControl);
        if ((!leftHandBound || !rightHandBound) && (SuperController.singleton.isOVR || SuperController.singleton.isOpenVR) && _waitForHandsCo == null)
            _waitForHandsCo = StartCoroutine(WaitForHandsCo());
        foreach (var motionControl in viveTrackers)
            Bind(motionControl);
    }

    private bool Bind(MotionControllerWithCustomPossessPoint motionControl)
    {
        var controllerWithSnapshot = FindController(motionControl);
        if (controllerWithSnapshot == null)
            return false;

        var controller = controllerWithSnapshot.controller;
        controllerWithSnapshot.snapshot = FreeControllerV3Snapshot.Snap(controller);

        if (motionControl.name == MotionControlNames.Head)
        {
            // Reduce the stretch between the head and the eye by matching the controller and the head bone
            controller.control.position = context.bones.First(b => b.name == "head").transform.position;
            if (motionControl.currentMotionControl == SuperController.singleton.centerCameraTarget.transform)
            {
                SuperController.singleton.AlignRigAndController(controller, motionControl);
            }
            else
            {
                SuperController.singleton.AlignTransformAndController(controller, motionControl);
            }
        }
        else
        {
            controller.control.SetPositionAndRotation(motionControl.controllerPointTransform.position, motionControl.controllerPointTransform.rotation);
        }

        controllerWithSnapshot.active = true;
        Possess(motionControl, controllerWithSnapshot.controller);

        if (motionControl.fingersTracking && controllerWithSnapshot.handControl != null)
            controllerWithSnapshot.handControl.possessed = true;

        return true;
    }

    private FreeControllerV3WithSnapshot FindController(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (!motionControl.enabled) return null;
        if (motionControl.mappedControllerName == null) return null;
        if (context.passenger.selectedJSON.val && motionControl.name == MotionControlNames.Head) return null;
        if (context.snug.selectedJSON.val && (motionControl.name == MotionControlNames.LeftHand || motionControl.name == MotionControlNames.RightHand)) return null;
        var controllerWithSnapshot = controllers.FirstOrDefault(cs => cs.controller.name == motionControl.mappedControllerName);
        if (controllerWithSnapshot == null) return null;
        var controller = controllerWithSnapshot.controller;
        if (controller.possessed) return null;
        if (!motionControl.SyncMotionControl()) return null;
        return controllerWithSnapshot;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_waitForHandsCo != null)
        {
            StopCoroutine(_waitForHandsCo);
            _waitForHandsCo = null;
        }

        foreach (var c in controllers)
        {
            if (!c.active) continue;

            c.controller.RestorePreLinkState();
            c.controller.possessed = false;

            var mac = c.controller.GetComponent<MotionAnimationControl>();
            if (mac != null)
            {
                mac.suspendPositionPlayback = false;
                mac.suspendRotationPlayback = false;
            }

            if (c.snapshot != null)
            {
                c.snapshot.Restore(restorePoseAfterPossessJSON.val);
                c.snapshot = null;
            }

            if (c.handControl != null)
                c.handControl.possessed = false;
        }

        if (_navigationRigSnapshot != null)
        {
            _navigationRigSnapshot.Restore();
            _navigationRigSnapshot = null;
        }
    }

    public void OnDestroy()
    {
        foreach (var c in motionControls)
        {
            if (c.trackerPointTransform != null)
                Destroy(c.trackerPointTransform.gameObject);
            if (c.controllerPointTransform != null)
                Destroy(c.controllerPointTransform.gameObject);
        }
    }

    private static void Possess(MotionControllerWithCustomPossessPoint motionControl, FreeControllerV3 controller)
    {
        var sc = SuperController.singleton;

        controller.possessed = true;

        var motionControllerRB = motionControl.controllerPointRB;
        var motionAnimationControl = controller.GetComponent<MotionAnimationControl>();

        controller.canGrabPosition = true;
        motionAnimationControl.suspendPositionPlayback = true;
        controller.RBHoldPositionSpring = sc.possessPositionSpring;

        controller.canGrabRotation = motionControl.controlRotation;
        motionAnimationControl.suspendRotationPlayback = true;
        controller.RBHoldRotationSpring = sc.possessRotationSpring;

        controller.SelectLinkToRigidbody(
            motionControllerRB,
            motionControl.controlRotation
                ? FreeControllerV3.SelectLinkState.PositionAndRotation
                : FreeControllerV3.SelectLinkState.Position
        );

        controller.canGrabPosition = false;
        controller.canGrabRotation = false;
    }

    private IEnumerator WaitForHandsCo()
    {
        yield return new WaitForSecondsRealtime(1f);
        while (!TryActivateHands())
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }
        _waitForHandsCo = null;
    }

    private bool TryActivateHands()
    {
        var leftActive = _leftHandController.active || Bind(leftHandMotionControl);
        var rightActive = _rightHandController.active || Bind(rightHandMotionControl);
        return leftActive && rightActive;
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        restorePoseAfterPossessJSON.StoreJSON(jc);

        var motionControlsJSON = new JSONClass();
        foreach (var motionControl in motionControls)
            motionControlsJSON[motionControl.name] = motionControl.GetJSON();
        jc["MotionControls"] = motionControlsJSON;
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        restorePoseAfterPossessJSON.RestoreFromJSON(jc);

        var motionControlsJSON = jc["MotionControls"].AsObject;
        foreach (var motionControlName in motionControlsJSON.Keys)
        {
            var motionControlJSON = motionControlsJSON[motionControlName];
            var motionControl = motionControls.FirstOrDefault(fc => fc.name == motionControlName);
            motionControl?.RestoreFromJSON(motionControlJSON);
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        foreach (var mc in context.trackers.motionControls)
            mc.ResetToDefault();

        previewTrackerOffsetJSON.SetValToDefault();
        restorePoseAfterPossessJSON.SetValToDefault();
    }
}
