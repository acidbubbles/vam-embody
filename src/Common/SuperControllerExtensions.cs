using UnityEngine;

public static class SuperControllerExtensions
{
    public static void AlignRigAndController(this SuperController sc, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl, bool alignControl = true)
    {
        var up = sc.navigationRig.up;

        sc.navigationRig.rotation = ComputeRotation(sc.navigationRig, controller, motionControl, up);

        if (alignControl)
            controller.AlignTo(motionControl.controllerPointTransform, true);

        var position = ComputePosition(sc.navigationRig, controller, motionControl);

        var upDelta = Vector3.Dot(position - sc.navigationRig.position, up);
        position += up * (0f - upDelta);
        sc.navigationRig.position = position;
        sc.playerHeightAdjust += upDelta;

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

    public static void AlignTransformAndController(this SuperController _, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl, bool alignControl = true)
    {
        var transform = motionControl.currentMotionControl;
        var up = transform.up;

        transform.rotation = ComputeRotation(transform, controller, motionControl, up);

        if (alignControl)
            controller.AlignTo(motionControl.controllerPointTransform, true);

        var position = ComputePosition(transform, controller, motionControl);

        transform.position = position;
    }

    private static Quaternion ComputeRotation(Transform transform, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl, Vector3 up)
    {
        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();

        var fromDirection = Vector3.ProjectOnPlane(transform.forward, up);
        var toDirection = Vector3.ProjectOnPlane(forwardPossessAxis, up);
        if (Vector3.Dot(upPossessAxis, up) < 0f && Vector3.Dot(transform.up, up) > 0f)
            toDirection = -toDirection;

        var rotation = Quaternion.FromToRotation(fromDirection, toDirection);
        return rotation * transform.rotation;
    }

    private static Vector3 ComputePosition(Transform transform, FreeControllerV3 controller, MotionControllerWithCustomPossessPoint motionControl)
    {
        var possessPointDelta = controller.control.position - motionControl.controllerPointTransform.position;
        var currentPosition = transform.position;
        return currentPosition + possessPointDelta;
    }
}
