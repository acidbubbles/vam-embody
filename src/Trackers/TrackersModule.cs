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
    private FreeControllerV3 headPossessedController;
    private Transform motionControllerHead => SuperController.singleton.centerCameraTarget.transform;


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
        HeadPossess(containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "headControl"), true, true, true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ClearPossess();
    }

    private void HeadPossess(FreeControllerV3 headPossess, bool alignRig = false, bool usePossessorSnapPoint = true, bool adjustSpring = true)
    {
        if (!headPossess.canGrabPosition && !headPossess.canGrabRotation)
        {
            return;
        }

        var component = motionControllerHead.GetComponent<Possessor>();
        var component2 = motionControllerHead.GetComponent<Rigidbody>();
        headPossessedController = headPossess;
        if (SuperController.singleton.headPossessedActivateTransform != null)
        {
            SuperController.singleton.headPossessedActivateTransform.gameObject.SetActive(true);
        }

        if (SuperController.singleton.headPossessedText != null)
        {
            if (headPossessedController.containingAtom != null)
            {
                SuperController.singleton.headPossessedText.text = headPossessedController.containingAtom.uid + ":" + headPossessedController.name;
            }
            else
            {
                SuperController.singleton.headPossessedText.text = headPossessedController.name;
            }
        }

        headPossessedController.possessed = true;
        if (headPossessedController.canGrabPosition)
        {
            var component3 = headPossessedController.GetComponent<MotionAnimationControl>();
            if (component3 != null)
            {
                component3.suspendPositionPlayback = true;
            }

            if (SuperController.singleton.allowPossessSpringAdjustment && adjustSpring)
            {
                headPossessedController.RBHoldPositionSpring = SuperController.singleton.possessPositionSpring;
            }
        }

        if (headPossessedController.canGrabRotation)
        {
            var component4 = headPossessedController.GetComponent<MotionAnimationControl>();
            if (component4 != null)
            {
                component4.suspendRotationPlayback = true;
            }

            if (SuperController.singleton.allowPossessSpringAdjustment && adjustSpring)
            {
                headPossessedController.RBHoldRotationSpring = SuperController.singleton.possessRotationSpring;
            }
        }

        SuperController.singleton.SyncMonitorRigPosition();
        if (alignRig)
        {
            AlignRigAndController(headPossessedController);
        }
        else if (component != null && component.autoSnapPoint != null && usePossessorSnapPoint)
        {
            headPossessedController.PossessMoveAndAlignTo(component.autoSnapPoint);
        }

        if (!(component2 != null))
        {
            return;
        }

        var linkState = FreeControllerV3.SelectLinkState.Position;
        if (headPossessedController.canGrabPosition)
        {
            if (headPossessedController.canGrabRotation)
            {
                linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
            }
        }
        else if (headPossessedController.canGrabRotation)
        {
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        }

        headPossessedController.SelectLinkToRigidbody(component2, linkState);
    }

    private void AlignRigAndController(FreeControllerV3 controller)
    {
        var navigationRig = SuperController.singleton.navigationRig;
        var component = motionControllerHead.GetComponent<Possessor>();
        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var up = navigationRig.up;
        var fromDirection = Vector3.ProjectOnPlane(motionControllerHead.forward, up);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRig.up);
        if (Vector3.Dot(upPossessAxis, up) < 0f && Vector3.Dot(motionControllerHead.up, up) > 0f)
        {
            vector = -vector;
        }

        var lhs = Quaternion.FromToRotation(fromDirection, vector);
        navigationRig.rotation = lhs * navigationRig.rotation;
        if (controller.canGrabRotation)
        {
            controller.AlignTo(component.autoSnapPoint, true);
        }

        var a = (!(controller.possessPoint != null)) ? controller.control.position : controller.possessPoint.position;
        var b = a - component.autoSnapPoint.position;
        var vector2 = navigationRig.position + b;
        var num = Vector3.Dot(vector2 - navigationRig.position, up);
        vector2 += up * (0f - num);
        navigationRig.position = vector2;
        SuperController.singleton.playerHeightAdjust += num;
        if (SuperController.singleton.MonitorCenterCamera != null)
        {
            SuperController.singleton.MonitorCenterCamera.transform.LookAt(controller.transform.position + forwardPossessAxis);
            Vector3 localEulerAngles = SuperController.singleton.MonitorCenterCamera.transform.localEulerAngles;
            localEulerAngles.y = 0f;
            localEulerAngles.z = 0f;
            SuperController.singleton.MonitorCenterCamera.transform.localEulerAngles = localEulerAngles;
        }

        headPossessedController.PossessMoveAndAlignTo(component.autoSnapPoint);
    }

    public void ClearPossess()
    {
        if (headPossessedController != null)
        {
            headPossessedController.RestorePreLinkState();
            headPossessedController.possessed = false;
            var component13 = headPossessedController.GetComponent<MotionAnimationControl>();
            if (component13 != null)
            {
                component13.suspendPositionPlayback = false;
                component13.suspendRotationPlayback = false;
            }

            headPossessedController = null;
            if (SuperController.singleton.headPossessedActivateTransform != null)
            {
                SuperController.singleton.headPossessedActivateTransform.gameObject.SetActive(false);
            }
        }
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
