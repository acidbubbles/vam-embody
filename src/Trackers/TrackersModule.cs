using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool importDefaultsOnLoad { get; }
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    JSONStorableBool previewTrackerOffsetJSON { get; }
    List<MotionControllerWithCustomPossessPoint> motionControls { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> viveTrackers { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> headAndHands { get; }
    MotionControllerWithCustomPossessPoint headMotionControl { get; }
    MotionControllerWithCustomPossessPoint leftHandMotionControl { get; }
    MotionControllerWithCustomPossessPoint rightHandMotionControl { get; }
    List<FreeControllerV3WithSnapshot> controllers { get; }
    void TryBindTrackers();
    void BindFingers();
    void ReleaseFingers();
    void ClearPersonalData();
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
    public JSONStorableBool importDefaultsOnLoad { get; } = new JSONStorableBool("ImportDefaultsOnLoad", true);
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    public JSONStorableBool previewTrackerOffsetJSON { get; } = new JSONStorableBool("PreviewTrackerOffset", false);

    private FreeControllerV3WithSnapshot _leftHandController;
    private FreeControllerV3WithSnapshot _rightHandController;

    public override void Awake()
    {
        base.Awake();

        headMotionControl = AddMotionControl(MotionControlNames.Head, () => context.head, "headControl");
        leftHandMotionControl = AddMotionControl(MotionControlNames.LeftHand, () => context.LeftHand(leftHandMotionControl?.useLeapPositioning ?? false), "lHandControl", mc => HandsAdjustments.ConfigureHand(mc, false));
        rightHandMotionControl = AddMotionControl(MotionControlNames.RightHand, () => context.RightHand(rightHandMotionControl?.useLeapPositioning ?? false), "rHandControl", mc => HandsAdjustments.ConfigureHand(mc, true));
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}1", () => context.viveTracker1 != null && context.viveTracker1.gameObject.activeInHierarchy ? context.viveTracker1 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}2", () => context.viveTracker2 != null && context.viveTracker2.gameObject.activeInHierarchy ? context.viveTracker2 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}3", () => context.viveTracker3 != null && context.viveTracker3.gameObject.activeInHierarchy ? context.viveTracker3 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}4", () => context.viveTracker4 != null && context.viveTracker4.gameObject.activeInHierarchy ? context.viveTracker4 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}5", () => context.viveTracker5 != null && context.viveTracker5.gameObject.activeInHierarchy ? context.viveTracker5 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}6", () => context.viveTracker6 != null && context.viveTracker6.gameObject.activeInHierarchy ? context.viveTracker6 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}7", () => context.viveTracker7 != null && context.viveTracker7.gameObject.activeInHierarchy ? context.viveTracker7 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}8", () => context.viveTracker8 != null && context.viveTracker8.gameObject.activeInHierarchy ? context.viveTracker8 : null);

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
                if (!val || mc.name != MotionControlNames.Head || mc.currentMotionControl != null && mc.currentMotionControl == context.diagnostics.head)
                {
                    mc.showPreview = val;
                }
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
            AdjustHeadToEyesOffset();
        }

        if (restorePoseAfterPossessJSON.val)
        {
            foreach (var controller in controllers)
            {
                if (controller.controller.control == null) continue;
                controller.snapshot = FreeControllerV3Snapshot.Snap(controller.controller);
            }
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

        TryBindTrackers();
    }

    public void TryBindTrackers()
    {
        if(Bind(headMotionControl))
            StartCoroutine(OnEnableCo(NavigationRigSnapshot.Snap()));
        Bind(leftHandMotionControl);
        Bind(rightHandMotionControl);
        BindFingers();
        foreach (var motionControl in viveTrackers)
            Bind(motionControl);
    }

    private IEnumerator OnEnableCo(NavigationRigSnapshot rigSnapshot)
    {
        // Once bound, prevent vam from moving the head elsewhere due to world scale change
        yield return new WaitForEndOfFrame();
        rigSnapshot.Restore();
        yield return 0f;
        rigSnapshot.Restore();
    }

    private bool Bind(MotionControllerWithCustomPossessPoint motionControl)
    {
        var controllerWithSnapshot = FindController(motionControl);
        if (controllerWithSnapshot == null)
            return false;
        if (controllerWithSnapshot.active)
            return false;

        var controller = controllerWithSnapshot.controller;

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
            if (motionControl.controlRotation)
                controller.control.rotation = motionControl.controllerPointTransform.rotation;
            if (motionControl.controlPosition)
                controller.control.position = motionControl.controllerPointTransform.position;
        }

        controllerWithSnapshot.active = true;
        Possess(motionControl, controllerWithSnapshot.controller);

        return true;
    }

    public void BindFingers()
    {
        if (leftHandMotionControl.fingersTracking && _leftHandController.handControl != null)
            _leftHandController.handControl.possessed = true;
        if (rightHandMotionControl.fingersTracking && _rightHandController.handControl != null)
            _rightHandController.handControl.possessed = true;
    }

    public void ReleaseFingers()
    {
        if (_leftHandController.handControl != null)
            _leftHandController.handControl.possessed = false;
        if (_rightHandController.handControl != null)
            _rightHandController.handControl.possessed = false;
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

        ReleaseFingers();

        foreach (var c in controllers)
        {
            if (c.active)
            {
                c.controller.RestorePreLinkState();
                c.controller.possessed = false;
                c.controller.startedPossess = false;

                var mac = c.controller.GetComponent<MotionAnimationControl>();
                if (mac != null)
                {
                    mac.suspendPositionPlayback = false;
                    mac.suspendRotationPlayback = false;
                }

                c.active = false;
            }

            if (c.snapshot != null)
            {
                c.snapshot.Restore(restorePoseAfterPossessJSON.val);
                c.snapshot = null;
            }
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
        if (!motionControl.controlPosition && !motionControl.controlRotation) return;

        var sc = SuperController.singleton;

        controller.possessed = true;

        var mac = controller.GetComponent<MotionAnimationControl>();
        if (mac != null)
        {
            if (motionControl.controlPosition)
                mac.suspendPositionPlayback = true;
            if (motionControl.controlRotation)
                mac.suspendRotationPlayback = true;
        }

        if (!motionControl.keepCurrentPhysicsHoldStrength)
        {
            if (motionControl.controlPosition)
                controller.RBHoldPositionSpring = sc.possessPositionSpring;
            if (motionControl.controlRotation)
                controller.RBHoldRotationSpring = sc.possessRotationSpring;
        }

        var motionControllerRB = motionControl.controllerPointRB;
        FreeControllerV3.SelectLinkState linkState;
        if (!motionControl.controlRotation)
            linkState = FreeControllerV3.SelectLinkState.Position;
        else if (!motionControl.controlPosition)
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        else
            linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
        controller.SelectLinkToRigidbody(
            motionControllerRB,
            linkState
        );

        controller.canGrabPosition = false;
        controller.canGrabRotation = false;
    }

    public void ClearPersonalData()
    {
        foreach (var mc in context.trackers.motionControls)
            mc.ResetToDefault(true);
    }

    public override void StoreJSON(JSONClass jc, bool includeProfile)
    {
        base.StoreJSON(jc, includeProfile);

        if (!includeProfile)
        {
            importDefaultsOnLoad.StoreJSON(jc);
        }

        if (!importDefaultsOnLoad.val || includeProfile)
        {
            restorePoseAfterPossessJSON.StoreJSON(jc);

            var motionControlsJSON = new JSONClass();
            foreach (var motionControl in motionControls)
                motionControlsJSON[motionControl.name] = motionControl.GetJSON();
            jc["MotionControls"] = motionControlsJSON;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromDefaults)
    {
        base.RestoreFromJSON(jc, fromDefaults);

        if (!fromDefaults)
        {
            importDefaultsOnLoad.RestoreFromJSON(jc);
        }

        if (importDefaultsOnLoad.val || !fromDefaults)
        {
            restorePoseAfterPossessJSON.RestoreFromJSON(jc);

            var motionControlsJSON = jc["MotionControls"].AsObject;
            foreach (var motionControlName in motionControlsJSON.Keys)
            {
                var motionControlJSON = motionControlsJSON[motionControlName];
                var motionControl = motionControls.FirstOrDefault(fc => fc.name == motionControlName);
                motionControl?.RestoreFromJSON(motionControlJSON);
            }
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        importDefaultsOnLoad.SetValToDefault();
        restorePoseAfterPossessJSON.SetValToDefault();
        previewTrackerOffsetJSON.SetValToDefault();

        foreach (var mc in context.trackers.motionControls)
            mc.ResetToDefault();
    }
}
