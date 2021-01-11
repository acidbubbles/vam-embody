using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ITrackersModule : IEmbodyModule
{
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;

    private FreeControllerV3 _headControl;
    private FreeControllerV3Snapshot _possessedHeadSnapshot;
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void Awake()
    {
        base.Awake();

        _headControl = context.headControl;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.ClearPossess();

        HeadPossess(_headControl);
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
        _possessedHeadSnapshot = FreeControllerV3Snapshot.Snap(controller);

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
        // _headPossessedController.PossessMoveAndAlignTo(possessor.autoSnapPoint);

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

    private void AlignRigAndController(FreeControllerV3 controller)
    {
        var sc = SuperController.singleton;
        var navigationRig = sc.navigationRig;
        var motionControllerHead = sc.centerCameraTarget.transform;
        var component = motionControllerHead.GetComponent<Possessor>();
        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var up = navigationRig.up;
        var fromDirection = Vector3.ProjectOnPlane(motionControllerHead.forward, up);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, up);
        if (Vector3.Dot(upPossessAxis, up) < 0f && Vector3.Dot(motionControllerHead.up, up) > 0f)
            vector = -vector;

        var rotation = Quaternion.FromToRotation(fromDirection, vector);
        navigationRig.rotation = rotation * navigationRig.rotation;
        if (controller.canGrabRotation)
            controller.AlignTo(component.autoSnapPoint, true);

        var a = (!(controller.possessPoint != null)) ? controller.control.position : controller.possessPoint.position;
        var b = a - component.autoSnapPoint.position;
        var navigationRigPosition = navigationRig.position;
        var vector2 = navigationRigPosition + b;
        var num = Vector3.Dot(vector2 - navigationRigPosition, up);
        vector2 += up * (0f - num);
        navigationRig.position = vector2;
        sc.playerHeightAdjust += num;
        if (sc.MonitorCenterCamera != null)
        {
            var monitorCenterCameraTransform = sc.MonitorCenterCamera.transform;
            monitorCenterCameraTransform.LookAt(controller.transform.position + forwardPossessAxis);
            var localEulerAngles = monitorCenterCameraTransform.localEulerAngles;
            localEulerAngles.y = 0f;
            localEulerAngles.z = 0f;
            monitorCenterCameraTransform.localEulerAngles = localEulerAngles;
        }

        controller.PossessMoveAndAlignTo(component.autoSnapPoint);
    }

    public void ClearPossess()
    {
        if (_possessedHeadSnapshot == null) return;

        _possessedHeadSnapshot.controller.RestorePreLinkState();
        _possessedHeadSnapshot.controller.possessed = false;
        var mac = _possessedHeadSnapshot.controller.GetComponent<MotionAnimationControl>();
        mac.suspendPositionPlayback = false;
        mac.suspendRotationPlayback = false;

        _possessedHeadSnapshot.Restore();
        _possessedHeadSnapshot = null;

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
