using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool restorePoseAfterPossessJSON { get; }
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public class FreeControllerV3WithCustomPossessPoint
    {
        public FreeControllerV3 controller;
        public Transform possessPoint;
        public FreeControllerV3Snapshot snapshot;
        public bool possessed = false;
        public string mappedMotionControl;
    }

    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public readonly List<FreeControllerV3WithCustomPossessPoint> customizedControllers = new List<FreeControllerV3WithCustomPossessPoint>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            var possessPointGameObject = new GameObject($"EmbodySnapPointFor{controller.name}");
            possessPointGameObject.transform.SetParent(controller.transform, false);
            if (controller.possessPoint != null)
            {
                possessPointGameObject.transform.localPosition = controller.possessPoint.localPosition;
                possessPointGameObject.transform.localRotation = controller.possessPoint.localRotation;
            }
            var rb = possessPointGameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
            string mappedMotionControl;
            switch (controller.name)
            {
                case "headControl":
                    mappedMotionControl = "head";
                    break;
                default:
                    mappedMotionControl = null;
                    break;
            }
            customizedControllers.Add(new FreeControllerV3WithCustomPossessPoint
            {
                controller = controller,
                possessPoint = possessPointGameObject.transform,
                mappedMotionControl =  mappedMotionControl
            });
        }
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
                case "head":
                    HeadPossess(c);
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
        foreach (var c in customizedControllers)
        {
            Destroy(c.possessPoint);
        }
    }

    private void HeadPossess(FreeControllerV3WithCustomPossessPoint customized)
    {
        var controller = customized.controller;

        if (!controller.canGrabPosition && !controller.canGrabRotation)
            return;

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

        customized.possessed = true;
        controller.possessed = true;

        var sc = SuperController.singleton;
        var motionControllerHead = sc.centerCameraTarget.transform;
        var motionControllerHeadRigidbody = motionControllerHead.GetComponent<Rigidbody>();

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
        AlignRigAndController(customized);

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

    private static void AlignRigAndController(FreeControllerV3WithCustomPossessPoint customized)
    {
        var controller = customized.controller;
        var sc = SuperController.singleton;
        var navigationRig = sc.navigationRig;
        var motionControllerHead = sc.centerCameraTarget.transform;
        // NOTE: This code comes from VaM
        var possessor = motionControllerHead.GetComponent<Possessor>();

        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var navigationRigUp = navigationRig.up;

        var fromDirection = Vector3.ProjectOnPlane(motionControllerHead.forward, navigationRigUp);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRigUp);
        if (Vector3.Dot(upPossessAxis, navigationRigUp) < 0f && Vector3.Dot(motionControllerHead.up, navigationRigUp) > 0f)
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
            controller.AlignTo(possessor.autoSnapPoint, true);

        var possessPointDelta = customized.possessPoint.position - possessor.autoSnapPoint.position;
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

        controller.PossessMoveAndAlignTo(possessor.autoSnapPoint);

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
                {"MotionControl", customized.mappedMotionControl},
                {"Position", customized.possessPoint.localPosition.ToJSON()},
                {"Rotation", customized.possessPoint.localEulerAngles.ToJSON()}
            };
            controllersJSON[customized.controller.name] = controllerJSON;
        }
        jc["Controllers"] = controllersJSON;
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
            customized.mappedMotionControl = controllerJSON["MotionControl"];
            customized.possessPoint.localPosition = controllerJSON["Position"].AsObject.ToVector3(Vector3.zero);
            customized.possessPoint.localEulerAngles = controllerJSON["MotionControl"].AsObject.ToVector3(Vector3.zero);
        }
    }
}
