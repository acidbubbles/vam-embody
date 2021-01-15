using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Animations;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    List<MotionControllerWithCustomPossessPoint> motionControls { get; }
    List<FreeControllerV3WithSnapshot> controllers { get; }
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public List<MotionControllerWithCustomPossessPoint> motionControls { get; } = new List<MotionControllerWithCustomPossessPoint>();
    public List<FreeControllerV3WithSnapshot> controllers { get; } = new List<FreeControllerV3WithSnapshot>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        // TODO: This actually changes from MonitorCamera to the actual VR headset when switching monitor mode. We can make a func, but then we'd need to switch the possess point's parent. SetParent(x, false) should keep local position.
        // TODO: Determine some reasonable offset value
        AddMotionControl("Head", () => context.head, "headControl");
        AddMotionControl("LeftHand", () => context.leftHand, "lHandControl", new Vector3(0.04120696f, 0, -0.0057684838f));
        AddMotionControl("RightHand", () => context.rightHand, "rHandControl", new Vector3(-0.04120696f, 0, -0.0057684838f));
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
            controllers.Add(new FreeControllerV3WithSnapshot
            {
                controller = controller,
            });
        }
    }

    private void AddMotionControl(string motionControlName, Func<Transform> getMotionControl, string mappedControllerName = null, Vector3 baseOffset = new Vector3())
    {
        var motionControl = MotionControllerWithCustomPossessPoint.Create(motionControlName, getMotionControl);
        motionControl.mappedControllerName = mappedControllerName;
        motionControl.baseOffset = baseOffset;
        motionControls.Add(motionControl);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        foreach (var motionControl in motionControls)
        {
            if (motionControl.mappedControllerName == null) continue;
            var controllerWithSnapshot = controllers.FirstOrDefault(cs => cs.controller.name == motionControl.mappedControllerName);
            if (controllerWithSnapshot == null) continue;
            var controller = controllerWithSnapshot.controller;
            if (controller.possessed) continue;
            if (restorePoseAfterPossessJSON.val)
                controllerWithSnapshot.snapshot = FreeControllerV3Snapshot.Snap(controller);
            if(!motionControl.Connect()) continue;

            if (motionControl.name == "Head")
            {
                // TODO: Find the eyes center and offset from this point instead. in the case of hands, find the possessPoint if it's available and use that as the base offset.
                // TODO: This is only useful on Desktop, we'll overwrite it anyway. Validate.
                // sc.SyncMonitorRigPosition();
                var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
                var eyesCenter = (eyes.First(eye => eye.name == "lEye").transform.position + eyes.First(eye => eye.name == "rEye").transform.position) / 2f;
                motionControl.baseOffset = -controller.transform.InverseTransformPoint(eyesCenter);
                if (motionControl.currentMotionControl == SuperController.singleton.centerCameraTarget.transform)
                {
                    _navigationRigSnapshot = NavigationRigSnapshot.Snap();
                    AlignRigAndController(controllerWithSnapshot, motionControl);
                }
                else
                {
                    // TODO: Cancel out the customPossessPoint
                    motionControl.currentMotionControl.SetPositionAndRotation(controller.control.position + controller.control.rotation * -motionControl.offset, controller.control.rotation);
                }
            }
            else
            {
                controller.control.SetPositionAndRotation(motionControl.customPossessPoint.position, motionControl.customPossessPoint.rotation);
                // TODO: Rotate around the "new" center and update in the UI
                // controller.control.RotateAround(motionControl.customPossessPoint.position, motionControl.customPossessPoint.up, 45f);
            }

            Possess(controllerWithSnapshot, motionControl);
            // SuperController.LogMessage($"{controller.transform.localRotation}");
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ClearPossess();
    }

    public void OnDestroy()
    {
        foreach (var c in motionControls)
        {
            if (c.customPossessPoint != null)
                Destroy(c.customPossessPoint.gameObject);
        }
    }

    private void Possess(FreeControllerV3WithSnapshot customized, MotionControllerWithCustomPossessPoint motionControl)
    {
        var sc = SuperController.singleton;
        var controller = customized.controller;

        if (!controller.canGrabPosition && !controller.canGrabRotation)
            return;

        controller.possessed = true;

        var motionControllerHeadRigidbody = motionControl.customRigidbody;

        if (controller.canGrabPosition)
        {
            controller.GetComponent<MotionAnimationControl>().suspendPositionPlayback = true;
            controller.RBHoldPositionSpring = sc.possessPositionSpring;
        }

        if (controller.canGrabRotation)
        {
             controller.GetComponent<MotionAnimationControl>().suspendRotationPlayback = true;
            controller.RBHoldRotationSpring = sc.possessRotationSpring;
        }

        var linkState = FreeControllerV3.SelectLinkState.Position;
        if (controller.canGrabPosition)
        {
            if (controller.canGrabRotation)
                linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
        }
        else if (controller.canGrabRotation)
        {
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        }

        controller.SelectLinkToRigidbody(motionControllerHeadRigidbody, linkState);
    }

    private static void AlignRigAndController(FreeControllerV3WithSnapshot customized, MotionControllerWithCustomPossessPoint motionControl)
    {
        var controller = customized.controller;
        var sc = SuperController.singleton;
        var navigationRig = sc.navigationRig;
        // NOTE: This code comes from VaM

        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var navigationRigUp = navigationRig.up;

        var fromDirection = Vector3.ProjectOnPlane(motionControl.customPossessPoint.forward, navigationRigUp);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRigUp);
        if (Vector3.Dot(upPossessAxis, navigationRigUp) < 0f && Vector3.Dot(motionControl.customPossessPoint.up, navigationRigUp) > 0f)
            vector = -vector;

        var rotation = Quaternion.FromToRotation(fromDirection, vector);
        navigationRig.rotation = rotation * navigationRig.rotation;

        var followWhenOffPosition = Vector3.zero;
        var followWhenOffRotation = Quaternion.identity;
        var followWhenOff = controller.followWhenOff;
        var useFollowWhenOffAndPossessPoint = controller.possessPoint != null && followWhenOff != null;
        if (useFollowWhenOffAndPossessPoint)
        {
            followWhenOffPosition = followWhenOff.position;
            followWhenOffRotation = followWhenOff.rotation;
            followWhenOff.position = controller.control.position;
            followWhenOff.rotation = controller.control.rotation;
        }

        if (controller.canGrabRotation)
            controller.AlignTo(motionControl.customPossessPoint, true);

        var possessPointPosition = controller.possessPoint == null ? controller.control.position : controller.possessPoint.position;
        var possessPointDelta = possessPointPosition - motionControl.customPossessPoint.position;
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

        controller.PossessMoveAndAlignTo(motionControl.customPossessPoint);

        if (useFollowWhenOffAndPossessPoint)
        {
            followWhenOff.position = followWhenOffPosition;
            followWhenOff.rotation = followWhenOffRotation;
        }
    }

    public void ClearPossess()
    {
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

            if (restorePoseAfterPossessJSON.val && c.snapshot != null)
            {
                c.snapshot.Restore();
                c.snapshot = null;
            }
        }

        if (_navigationRigSnapshot != null)
        {
            _navigationRigSnapshot.Restore();
            _navigationRigSnapshot = null;
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
                {"OffsetPosition", customized.customPossessPoint.localPosition.ToJSON()},
                {"OffsetRotation", customized.customPossessPoint.localEulerAngles.ToJSON()},
                {"Controller", customized.mappedControllerName}
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
            customized.customPossessPoint.localPosition = controllerJSON["OffsetPosition"].AsObject.ToVector3(Vector3.zero);
            customized.customPossessPoint.localEulerAngles = controllerJSON["OffsetRotation"].AsObject.ToVector3(Vector3.zero);
            customized.mappedControllerName = controllerJSON["Controller"].Value;
            if (customized.mappedControllerName == "") customized.mappedControllerName = null;
        }
    }
}
