#define VAM_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Passenger Version 0.0.0
/// Your eyes follow the model's eyes, not the other way around
/// </summary>
public class Passenger : MVRScript
{
    private FreeControllerV3 _headControl;
    private Possessor _possessor;

    public override void Init()
    {
        try
        {
            _headControl = (FreeControllerV3)containingAtom.GetStorableByID("headControl");
            _possessor = SuperController
                .FindObjectsOfType(typeof(Possessor))
                .Where(p => p.name == "CenterEye")
                .Select(p => p as Possessor)
                .FirstOrDefault();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize plugin: " + e);
        }
    }

    public void LateUpdate()
    {
        var controller = _headControl;

        Possessor component = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
        Vector3 forwardPossessAxis = controller.GetForwardPossessAxis();
        Vector3 upPossessAxis = controller.GetUpPossessAxis();
        Vector3 up = SuperController.singleton.navigationRig.up;
        Vector3 fromDirection = Vector3.ProjectOnPlane(SuperController.singleton.centerCameraTarget.transform.forward, up);
        Vector3 toDirection = Vector3.ProjectOnPlane(forwardPossessAxis, SuperController.singleton.navigationRig.up);
        if ((double)Vector3.Dot(upPossessAxis, up) < 0.0 && (double)Vector3.Dot(SuperController.singleton.centerCameraTarget.transform.up, up) > 0.0)
            toDirection = -toDirection;
        SuperController.singleton.navigationRig.rotation = Quaternion.FromToRotation(fromDirection, toDirection) * SuperController.singleton.navigationRig.rotation;
        if (controller.canGrabRotation)
            controller.AlignTo(component.autoSnapPoint, true);
        Vector3 vector3 = SuperController.singleton.navigationRig.position + ((!((UnityEngine.Object)controller.possessPoint != (UnityEngine.Object)null) ? controller.control.position : controller.possessPoint.position) - component.autoSnapPoint.position);
        float num = Vector3.Dot(vector3 - SuperController.singleton.navigationRig.position, up);
        SuperController.singleton.navigationRig.position = vector3 + up * -num;
        SuperController.singleton.playerHeightAdjust += num;
        if ((UnityEngine.Object)SuperController.singleton.MonitorCenterCamera != (UnityEngine.Object)null)
        {
            SuperController.singleton.MonitorCenterCamera.transform.LookAt(controller.transform.position + forwardPossessAxis);
            Vector3 localEulerAngles = SuperController.singleton.MonitorCenterCamera.transform.localEulerAngles;
            localEulerAngles.y = 0.0f;
            localEulerAngles.z = 0.0f;
            SuperController.singleton.MonitorCenterCamera.transform.localEulerAngles = localEulerAngles;
        }
        controller.PossessMoveAndAlignTo(component.autoSnapPoint);
    }
}
