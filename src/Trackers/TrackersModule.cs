using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class MotionControllerWithCustomPossessPoint
{
    public string name;
    public Transform customPossessPoint;
    public Rigidbody customRigidbody;
}

public class FreeControllerV3WithCustomPossessPoint
{
    public FreeControllerV3 controller;
    public FreeControllerV3Snapshot snapshot;
    public bool possessed;
    public string mappedMotionControl;
}

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    List<MotionControllerWithCustomPossessPoint> customizedMotionControls { get; }
    List<FreeControllerV3WithCustomPossessPoint> customizedControllers { get; }
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public List<MotionControllerWithCustomPossessPoint> customizedMotionControls { get; } = new List<MotionControllerWithCustomPossessPoint>();
    public List<FreeControllerV3WithCustomPossessPoint> customizedControllers { get; } = new List<FreeControllerV3WithCustomPossessPoint>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        AddMotionControl("Head", SuperController.singleton.centerCameraTarget.transform);
        AddMotionControl("LeftHand", SuperController.singleton.leftHand);
        AddMotionControl("RightHand", SuperController.singleton.rightHand);
        AddMotionControl("ViveTracker1", SuperController.singleton.viveTracker1);
        AddMotionControl("ViveTracker2", SuperController.singleton.viveTracker2);
        AddMotionControl("ViveTracker3", SuperController.singleton.viveTracker3);
        AddMotionControl("ViveTracker4", SuperController.singleton.viveTracker4);
        AddMotionControl("ViveTracker5", SuperController.singleton.viveTracker5);
        AddMotionControl("ViveTracker6", SuperController.singleton.viveTracker6);
        AddMotionControl("ViveTracker7", SuperController.singleton.viveTracker7);
        AddMotionControl("ViveTracker8", SuperController.singleton.viveTracker8);

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            string mappedMotionControl;
            switch (controller.name)
            {
                case "headControl":
                    mappedMotionControl = "Head";
                    break;
                default:
                    mappedMotionControl = null;
                    break;
            }
            customizedControllers.Add(new FreeControllerV3WithCustomPossessPoint
            {
                controller = controller,
                mappedMotionControl =  mappedMotionControl
            });
        }
    }

    private void AddMotionControl(string motionControlName, Transform motionControlTransform)
    {
        if (motionControlTransform == null) return;

        var possessPointGameObject = new GameObject($"EmbodyPossessPoint_{motionControlName}");
        possessPointGameObject.transform.SetParent(motionControlTransform, false);
        var rb = possessPointGameObject.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        this.customizedMotionControls.Add(new MotionControllerWithCustomPossessPoint
        {
            name = motionControlName,
            customPossessPoint = possessPointGameObject.transform,
            customRigidbody = rb
        });
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        if (restorePoseAfterPossessJSON.val)
        {
            foreach (var c in customizedControllers)
            {
                c.snapshot = FreeControllerV3Snapshot.Snap(c.controller);
            }
        }

        foreach (var c in customizedControllers)
        {
            switch (c.mappedMotionControl)
            {
                case "Head":
                    HeadPossess(c, customizedMotionControls.FirstOrDefault(x => x.name == c.mappedMotionControl));
                    break;
            }
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ClearPossess();
    }

    public void OnDestroy()
    {
        foreach (var c in customizedMotionControls)
        {
            Destroy(c.customPossessPoint);
        }
    }

    private void HeadPossess(FreeControllerV3WithCustomPossessPoint customized, MotionControllerWithCustomPossessPoint motionControl)
    {
        var sc = SuperController.singleton;
        var controller = customized.controller;

        if (!controller.canGrabPosition && !controller.canGrabRotation)
            return;

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

        customized.possessed = true;
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

        sc.SyncMonitorRigPosition();
        AlignRigAndController(customized, motionControl);

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

    private static void AlignRigAndController(FreeControllerV3WithCustomPossessPoint customized, MotionControllerWithCustomPossessPoint motionControl)
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
        foreach (var c in customizedControllers)
        {
            if (c.snapshot == null) continue;
            if (!c.possessed) continue;
            if (!c.controller.possessed) continue;

            c.controller.RestorePreLinkState();
            c.controller.possessed = false;
            var mac = c.controller.GetComponent<MotionAnimationControl>();
            mac.suspendPositionPlayback = false;
            mac.suspendRotationPlayback = false;

            if (restorePoseAfterPossessJSON.val)
                c.snapshot.Restore();
            c.snapshot = null;
        }

        _navigationRigSnapshot.Restore();
        _navigationRigSnapshot = null;
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        restorePoseAfterPossessJSON.StoreJSON(jc);

        var controllersJSON = new JSONClass();
        foreach (var customized in customizedControllers)
        {
            var controllerJSON = new JSONClass
            {
                {"MotionControl", customized.mappedMotionControl}
            };
            controllersJSON[customized.controller.name] = controllerJSON;
        }
        jc["Controllers"] = controllersJSON;

        var motionControlsJSON = new JSONClass();
        foreach (var customized in customizedMotionControls)
        {
            var motionControlJSON = new JSONClass
            {
                {"OffsetPosition", customized.customPossessPoint.localPosition.ToJSON()},
                {"OffsetRotation", customized.customPossessPoint.localEulerAngles.ToJSON()}
            };
            motionControlsJSON[customized.name] = motionControlJSON;
        }
        jc["MotionControl"] = motionControlsJSON;
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        restorePoseAfterPossessJSON.RestoreFromJSON(jc);

        var controllersJSON = jc["Controllers"].AsObject;
        foreach (var controllerName in controllersJSON.Keys)
        {
            var controllerJSON = controllersJSON[controllerName];
            var customized = customizedControllers.FirstOrDefault(fc => fc.controller.name == controllerName);
            if (customized == null) continue;
            customized.mappedMotionControl = controllerJSON["MotionControl"].Value;
        }

        var motionControlsJSON = jc["MotionControls"].AsObject;
        foreach (var motionControlName in motionControlsJSON.Keys)
        {
            var controllerJSON = motionControlsJSON[motionControlName];
            var customized = customizedMotionControls.FirstOrDefault(fc => fc.name == motionControlName);
            if (customized == null) continue;
            customized.customPossessPoint.localPosition = controllerJSON["Position"].AsObject.ToVector3(Vector3.zero);
            customized.customPossessPoint.localEulerAngles = controllerJSON["MotionControl"].AsObject.ToVector3(Vector3.zero);
        }
    }
}
