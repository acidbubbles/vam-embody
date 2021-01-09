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
    private FreeControllerV3 _headPossessedController;

    public override void Awake()
    {
        base.Awake();

        _headControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "headControl");
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (_headControl.possessed || _headControl.startedPossess)
            SuperController.singleton.ClearPossess();


        // TODO: Do the parenting, see how vam does it (however we might want to allow offsets)
        HeadPossess(containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "headControl"));
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ClearPossess();
    }

    private void HeadPossess(FreeControllerV3 headPossess)
    {
        if (!headPossess.canGrabPosition && !headPossess.canGrabRotation)
            return;

        _headPossessedController = headPossess;
        headPossess.possessed = true;

        var sc = SuperController.singleton;
        var motionControllerHead = sc.centerCameraTarget.transform;
        var motionControllerHeadRigidbody = motionControllerHead.GetComponent<Rigidbody>();

        if (_headPossessedController.canGrabPosition)
        {
            _headPossessedController.GetComponent<MotionAnimationControl>().suspendPositionPlayback = true;
            _headPossessedController.RBHoldPositionSpring = sc.possessPositionSpring;
        }

        if (_headPossessedController.canGrabRotation)
        {
             _headPossessedController.GetComponent<MotionAnimationControl>().suspendRotationPlayback = true;
            _headPossessedController.RBHoldRotationSpring = sc.possessRotationSpring;
        }

        sc.SyncMonitorRigPosition();
        AlignRigAndController(_headPossessedController);
        // _headPossessedController.PossessMoveAndAlignTo(possessor.autoSnapPoint);

        if (!(motionControllerHeadRigidbody != null))
        {
            return;
        }

        var linkState = FreeControllerV3.SelectLinkState.Position;
        if (_headPossessedController.canGrabPosition)
        {
            if (_headPossessedController.canGrabRotation)
                linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
        }
        else if (_headPossessedController.canGrabRotation)
        {
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        }

        _headPossessedController.SelectLinkToRigidbody(motionControllerHeadRigidbody, linkState);
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

        _headPossessedController.PossessMoveAndAlignTo(component.autoSnapPoint);
    }

    public void ClearPossess()
    {
        if (_headPossessedController == null) return;

        _headPossessedController.RestorePreLinkState();
        _headPossessedController.possessed = false;
        var mac = _headPossessedController.GetComponent<MotionAnimationControl>();
        mac.suspendPositionPlayback = false;
        mac.suspendRotationPlayback = false;

        _headPossessedController = null;
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
