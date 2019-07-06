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
    private FreeControllerV3 _selectedControl;
    private Possessor _possessor;
    private JSONStorableStringChooser _targetControllerJSON;
    private JSONStorableBool _activeJSON;

    public override void Init()
    {
        try
        {
            _possessor = SuperController
                .FindObjectsOfType(typeof(Possessor))
                .Where(p => p.name == "CenterEye")
                .Select(p => p as Possessor)
                .FirstOrDefault();

            InitControls();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize plugin: " + e);
        }
    }

    private void InitControls()
    {
        var targetControllers = containingAtom.freeControllers.Select(c => c.storeId).ToList();

        var defaultController = containingAtom.type == "Person" ? "headControl" : "control";
        _targetControllerJSON = new JSONStorableStringChooser("Target Controller", targetControllers, "headControl", "Target Controller: ", OnTargetControllerChanged);
        _targetControllerJSON.storeType = JSONStorableParam.StoreType.Physical;
        RegisterStringChooser(_targetControllerJSON);

        var targetControllerPopup = CreateScrollablePopup(_targetControllerJSON, false);
        targetControllerPopup.popupPanelHeight = 300f;

        if (!string.IsNullOrEmpty(_targetControllerJSON.val))
            OnTargetControllerChanged(_targetControllerJSON.val);

        _activeJSON = new JSONStorableBool("Active", false);
        RegisterBool(_activeJSON);

        var activeToggle = CreateToggle(_activeJSON, true);
    }

    private void OnTargetControllerChanged(string targetControllerStoreId)
    {
        if (containingAtom == null) throw new NullReferenceException("containingAtom");

        _selectedControl = containingAtom.freeControllers.FirstOrDefault(c => c.storeId == targetControllerStoreId);
        if (_selectedControl == null)
        {
            SuperController.LogError("Controller does not exist: " + targetControllerStoreId);
            return;
        }
        SuperController.LogMessage("Changed: " + _selectedControl.storeId);
    }

    public void LateUpdate()
    {
        if (!_activeJSON.val || _selectedControl == null) return;

        var controller = _selectedControl;

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
