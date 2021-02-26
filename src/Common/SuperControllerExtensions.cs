using UnityEngine;

public static class SuperControllerExtensions
{
    public static void AlignRigAndController(this SuperController sc, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl, bool alignControl = true)
    {
        sc.AlignToController(sc.navigationRig, controller, motionControl, alignControl);

        if (sc.MonitorCenterCamera != null)
        {
            var monitorCenterCameraTransform = sc.MonitorCenterCamera.transform;
            monitorCenterCameraTransform.LookAt(controller.transform.position + controller.GetForwardPossessAxis());
            var localEulerAngles = monitorCenterCameraTransform.localEulerAngles;
            localEulerAngles.y = 0f;
            localEulerAngles.z = 0f;
            monitorCenterCameraTransform.localEulerAngles = localEulerAngles;
        }
    }

    public static void AlignToController(this SuperController sc, Transform navigationRig, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl, bool alignControl = true)
    {
        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();
        var navigationRigUp = navigationRig.up;

        var fromDirection = Vector3.ProjectOnPlane(motionControl.controllerPointTransform.forward, navigationRigUp);
        var vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRigUp);
        if (Vector3.Dot(upPossessAxis, navigationRigUp) < 0f && Vector3.Dot(motionControl.controllerPointTransform.up, navigationRigUp) > 0f)
            vector = -vector;

        var rotation = Quaternion.FromToRotation(fromDirection, vector);
        navigationRig.rotation = rotation * navigationRig.rotation;

        if (alignControl)
            controller.AlignTo(motionControl.controllerPointTransform, true);

        var possessPointDelta = controller.control.position - motionControl.controllerPointTransform.position;
        var navigationRigPosition = navigationRig.position;
        var navigationRigPositionDelta = navigationRigPosition + possessPointDelta;
        var navigationRigUpDelta = Vector3.Dot(navigationRigPositionDelta - navigationRigPosition, navigationRigUp);
        navigationRigPositionDelta += navigationRigUp * (0f - navigationRigUpDelta);
        navigationRig.position = navigationRigPositionDelta;
        sc.playerHeightAdjust += navigationRigUpDelta;
    }
}
