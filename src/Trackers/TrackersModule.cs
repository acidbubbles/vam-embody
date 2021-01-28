using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    JSONStorableBool previewTrackerOffsetJSON { get; }
    List<MotionControllerWithCustomPossessPoint> motionControls { get; }
    List<FreeControllerV3WithSnapshot> controllers { get; }
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public IPassengerModule passenger { get; set; }
    public ISnugModule snug { get; set; }

    public List<MotionControllerWithCustomPossessPoint> motionControls { get; } = new List<MotionControllerWithCustomPossessPoint>();
    public List<FreeControllerV3WithSnapshot> controllers { get; } = new List<FreeControllerV3WithSnapshot>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    public JSONStorableBool previewTrackerOffsetJSON { get; } = new JSONStorableBool("PreviewTrackerOffset", false);
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        AddMotionControl(MotionControlNames.Head, () => context.head, "headControl");
        AddMotionControl(MotionControlNames.LeftHand, () => context.leftHand, "lHandControl", new Vector3(-0.03247f, -0.03789f, -0.10116f), new Vector3(-90, 90, 0));
        AddMotionControl(MotionControlNames.RightHand, () => context.rightHand, "rHandControl", new Vector3(0.03247f, -0.03789f, -0.10116f), new Vector3(-90, -90, 0));
        AddMotionControl("ViveTracker1", () => context.viveTracker1);
        AddMotionControl("ViveTracker2", () => context.viveTracker2);
        AddMotionControl("ViveTracker3", () => context.viveTracker3);
        AddMotionControl("ViveTracker4", () => context.viveTracker4);
        AddMotionControl("ViveTracker5", () => context.viveTracker5);
        AddMotionControl("ViveTracker6", () => context.viveTracker6);
        AddMotionControl("ViveTracker7", () => context.viveTracker7);
        AddMotionControl("ViveTracker8", () => context.viveTracker8);

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            controllers.Add(new FreeControllerV3WithSnapshot(controller));
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

    private void AddMotionControl(string motionControlName, Func<Transform> getMotionControl, string mappedControllerName = null, Vector3 baseOffset = new Vector3(), Vector3 baseOffsetRotation = new Vector3())
    {
        var motionControl = MotionControllerWithCustomPossessPoint.Create(motionControlName, getMotionControl);
        motionControl.mappedControllerName = mappedControllerName;
        motionControl.baseOffset = baseOffset;
        motionControl.baseOffsetRotation = baseOffsetRotation;
        motionControls.Add(motionControl);
        motionControl.SyncMotionControl();
    }

    public override bool BeforeEnable()
    {
        AdjustHeadToEyesOffset();
        return true;
    }

    private void AdjustHeadToEyesOffset()
    {
        var motionControl = motionControls.First(mc => mc.name == MotionControlNames.Head);
        var controller = FindController(motionControl)?.controller;
        if (controller == null) return;

        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var eyesCenter = (eyes.First(eye => eye.name == "lEye").transform.position + eyes.First(eye => eye.name == "rEye").transform.position) / 2f;
        motionControl.baseOffset = -controller.control.InverseTransformPoint(eyesCenter);
        SuperController.LogMessage(motionControl.baseOffset.ToString());
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        foreach (var motionControl in motionControls)
        {
            var controllerWithSnapshot = FindController(motionControl);
            if (controllerWithSnapshot == null) continue;
            var controller = controllerWithSnapshot.controller;
            if (restorePoseAfterPossessJSON.val)
                controllerWithSnapshot.snapshot = FreeControllerV3Snapshot.Snap(controller);

            if (motionControl.name == MotionControlNames.Head)
            {
                if (motionControl.currentMotionControl == SuperController.singleton.centerCameraTarget.transform)
                {
                    _navigationRigSnapshot = NavigationRigSnapshot.Snap();
                    AlignRigAndController(controller, motionControl);
                }
                else
                {
                    var controlRotation = controller.control.rotation;
                    motionControl.currentMotionControl.SetPositionAndRotation(
                        controller.control.position - controlRotation * motionControl.combinedOffset,
                        controlRotation
                    );
                }
            }
            else
            {
                controller.control.SetPositionAndRotation(motionControl.possessPointTransform.position, motionControl.possessPointTransform.rotation);
                if (controllerWithSnapshot.handControl != null)
                    controllerWithSnapshot.handControl.possessed = true;
            }

            Possess(motionControl, controllerWithSnapshot.controller);
        }
    }

    private FreeControllerV3WithSnapshot FindController(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (!motionControl.enabled) return null;
        if (motionControl.mappedControllerName == null) return null;
        if (passenger.selectedJSON.val && motionControl.name == MotionControlNames.Head) return null;
        if (snug.selectedJSON.val && (motionControl.name == MotionControlNames.LeftHand || motionControl.name == MotionControlNames.RightHand)) return null;
        var controllerWithSnapshot = controllers.FirstOrDefault(cs => cs.controller.name == motionControl.mappedControllerName);
        if (controllerWithSnapshot == null){SuperController.LogError("A"); return null;}
        var controller = controllerWithSnapshot.controller;
        if (controller.possessed) {SuperController.LogError("B");return null;}
        if (!controller.possessable) {SuperController.LogError("C");return null;}
        if (!motionControl.SyncMotionControl()) return null;
        return controllerWithSnapshot;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        foreach (var c in controllers)
        {
            if (!c.controller.possessed) continue;

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
                if (restorePoseAfterPossessJSON.val)
                    c.snapshot.Restore();
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
            if (c.offsetTransform != null)
                Destroy(c.offsetTransform.gameObject);
            if (c.possessPointTransform != null)
                Destroy(c.possessPointTransform.gameObject);
        }
    }

    private static void Possess(MotionControllerWithCustomPossessPoint motionControl, FreeControllerV3 controller)
    {
        var sc = SuperController.singleton;

        controller.possessed = true;

        var motionControllerHeadRigidbody = motionControl.customRigidbody;
        var motionAnimationControl = controller.GetComponent<MotionAnimationControl>();

        controller.canGrabPosition = true;
        motionAnimationControl.suspendPositionPlayback = true;
        controller.RBHoldPositionSpring = sc.possessPositionSpring;

        controller.canGrabRotation = true;
        motionAnimationControl.suspendRotationPlayback = true;
        controller.RBHoldRotationSpring = sc.possessRotationSpring;

        controller.SelectLinkToRigidbody(motionControllerHeadRigidbody, FreeControllerV3.SelectLinkState.PositionAndRotation);
    }

    private static void AlignRigAndController(FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl)
    {
        var sc = SuperController.singleton;
        var navigationRig = sc.navigationRig;

        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var navigationRigUp = navigationRig.up;

        var fromDirection = Vector3.ProjectOnPlane(motionControl.possessPointTransform.forward, navigationRigUp);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRigUp);
        if (Vector3.Dot(upPossessAxis, navigationRigUp) < 0f && Vector3.Dot(motionControl.possessPointTransform.up, navigationRigUp) > 0f)
            vector = -vector;

        var rotation = Quaternion.FromToRotation(fromDirection, vector);
        navigationRig.rotation = rotation * navigationRig.rotation;

        controller.AlignTo(motionControl.possessPointTransform, true);

        var possessPointDelta = controller.control.position - motionControl.currentMotionControl.position - controller.control.rotation * motionControl.combinedOffset;
        var navigationRigPosition = navigationRig.position;
        var navigationRigPositionDelta = navigationRigPosition + possessPointDelta;
        var navigationRigUpDelta = Vector3.Dot(navigationRigPositionDelta - navigationRigPosition, navigationRigUp);
        navigationRigPositionDelta += navigationRigUp * (0f - navigationRigUpDelta);
        navigationRig.position = navigationRigPositionDelta;
        sc.playerHeightAdjust += navigationRigUpDelta;

        if (sc.MonitorCenterCamera != null)
        {
            var monitorCenterCameraTransform = sc.MonitorCenterCamera.transform;
            monitorCenterCameraTransform.LookAt(controller.transform.position + forwardPossessAxis);
            var localEulerAngles = monitorCenterCameraTransform.localEulerAngles;
            localEulerAngles.y = 0f;
            localEulerAngles.z = 0f;
            monitorCenterCameraTransform.localEulerAngles = localEulerAngles;
        }
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        restorePoseAfterPossessJSON.StoreJSON(jc);

        var motionControlsJSON = new JSONClass();
        foreach (var customized in motionControls)
        {
            var motionControlJSON = new JSONClass
            {
                {"OffsetPosition", customized.possessPointTransform.localPosition.ToJSON()},
                {"OffsetRotation", customized.possessPointTransform.localEulerAngles.ToJSON()},
                {"Controller", customized.mappedControllerName},
                {"Enabled", customized.enabled ? "true" : "false"}
            };
            motionControlsJSON[customized.name] = motionControlJSON;
        }
        jc["MotionControls"] = motionControlsJSON;
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        restorePoseAfterPossessJSON.RestoreFromJSON(jc);

        var motionControlsJSON = jc["MotionControls"].AsObject;
        foreach (var motionControlName in motionControlsJSON.Keys)
        {
            var controllerJSON = motionControlsJSON[motionControlName];
            var customized = motionControls.FirstOrDefault(fc => fc.name == motionControlName);
            if (customized == null) continue;
            customized.possessPointTransform.localPosition = controllerJSON["OffsetPosition"].AsObject.ToVector3(Vector3.zero);
            customized.possessPointTransform.localEulerAngles = controllerJSON["OffsetRotation"].AsObject.ToVector3(Vector3.zero);
            customized.mappedControllerName = controllerJSON["Controller"].Value;
            customized.enabled = controllerJSON["Enabled"].Value != "false";
            if (customized.mappedControllerName == "") customized.mappedControllerName = null;
        }
    }
}
