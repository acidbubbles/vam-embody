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
    private FreeControllerV3 _selectedController;
    private Possessor _possessor;
    private JSONStorableStringChooser _targetControllerJSON;
    private JSONStorableBool _activeJSON;

    public override void Init()
    {
        try
        {
            _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

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
        _targetControllerJSON = new JSONStorableStringChooser("Target Controller", targetControllers, defaultController, "Target Controller: ", OnTargetControllerChanged);
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

        _selectedController = containingAtom.freeControllers.FirstOrDefault(c => c.storeId == targetControllerStoreId);
        if (_selectedController == null)

            SuperController.LogError("Controller does not exist: " + targetControllerStoreId);
    }

    public void LateUpdate()
    {
        if (!_activeJSON.val || _selectedController == null) return;

        var controller = _selectedController;
        var superController = SuperController.singleton;
        var navigationRig = superController.navigationRig;
        var centerCameraTarget = superController.centerCameraTarget;
        var monitorCenterCamera = superController.MonitorCenterCamera;

        var forwardPossessAxis = controller.GetForwardPossessAxis();
        var upPossessAxis = controller.GetUpPossessAxis();

        var up = navigationRig.up;

        var fromDirection = Vector3.ProjectOnPlane(centerCameraTarget.transform.forward, up);
        var toDirection = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRig.up);
        if (Vector3.Dot(upPossessAxis, up) < 0f && Vector3.Dot(centerCameraTarget.transform.up, up) > 0f)
            toDirection = -toDirection;

        navigationRig.rotation = Quaternion.FromToRotation(fromDirection, toDirection) * navigationRig.rotation;

        if (controller.canGrabRotation)
            controller.AlignTo(_possessor.autoSnapPoint, true);

        var positionOffset = navigationRig.position + ((controller.possessPoint == null ? controller.control.position : controller.possessPoint.position) - _possessor.autoSnapPoint.position);
        var playerHeightAdjustOffset = Vector3.Dot(positionOffset - navigationRig.position, up);
        navigationRig.position = positionOffset + up * -playerHeightAdjustOffset;
        superController.playerHeightAdjust += playerHeightAdjustOffset;

        if (monitorCenterCamera != null)
        {
            monitorCenterCamera.transform.LookAt(controller.transform.position + forwardPossessAxis);
            var localEulerAngles = monitorCenterCamera.transform.localEulerAngles;
            localEulerAngles.y = 0.0f;
            localEulerAngles.z = 0.0f;
            monitorCenterCamera.transform.localEulerAngles = localEulerAngles;
        }

        controller.PossessMoveAndAlignTo(_possessor.autoSnapPoint);
    }
}
