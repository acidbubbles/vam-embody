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
    public class FreeControllerV3WithSnapPoint
    {
        public FreeControllerV3 controller;
        public Transform snapPoint;
        public FreeControllerV3Snapshot snapshot;
    }

    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public readonly List<FreeControllerV3WithSnapPoint> map = new List<FreeControllerV3WithSnapPoint>();
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
        {
            map.Add(new FreeControllerV3WithSnapPoint
            {
                controller = controller,
            });
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        if (restorePoseAfterPossessJSON.val)
        {
            foreach (var c in map)
            {
                c.snapshot = FreeControllerV3Snapshot.Snap(c.controller);
            }
        }

        HeadPossess(map.First(c => c.controller.name == "headControl").controller);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ClearPossess();
    }

    private void HeadPossess(FreeControllerV3 controller)
    {
        if (!controller.canGrabPosition && !controller.canGrabRotation)
            return;

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

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
        AlignRigAndController(controller);

        if (!(motionControllerHeadRigidbody != null))
        {
            return;
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

    private static void AlignRigAndController(FreeControllerV3 controller)
    {
        // NOTE: This code comes from VaM
        var sc = SuperController.singleton;
        var navigationRig = sc.navigationRig;
        var motionControllerHead = sc.centerCameraTarget.transform;
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

        var possessPointPosition = controller.possessPoint == null ? controller.control.position : controller.possessPoint.position;
        var possessPointDelta = possessPointPosition - possessor.autoSnapPoint.position;
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
        foreach (var c in map)
        {
            if (c.snapshot == null) continue;
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
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);
    }
}
