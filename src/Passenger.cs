#define VAM_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Passenger Version 0.0.0
/// Your eyes follow the model's eyes, not the other way around
/// </summary>
public class Passenger : MVRScript
{
    private Rigidbody _link;
    private Possessor _possessor;
    private JSONStorableBool _activeJSON;
    private JSONStorableBool _rotationLockJSON;
    private JSONStorableBool _rotationLockNoRollJSON;
    private JSONStorableFloat _rotationSmoothingJSON;
    private JSONStorableFloat _rotationOffsetXJSON;
    private JSONStorableFloat _rotationOffsetYJSON;
    private JSONStorableFloat _rotationOffsetZJSON;
    private JSONStorableBool _positionLockJSON;
    private JSONStorableFloat _positionSmoothingJSON;
    private JSONStorableFloat _positionOffsetXJSON;
    private JSONStorableFloat _positionOffsetYJSON;
    private JSONStorableFloat _positionOffsetZJSON;
    private JSONStorableStringChooser _linkJSON;
    private Vector3 _previousPosition;
    private float _previousPlayerHeight;
    private Quaternion _previousRotation;
    private bool _active;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;

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
        const bool LeftSide = false;
        const bool RightSide = true;

        // Left Side

        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Target Controller: ", OnLinkChanged);
        _linkJSON.storeType = JSONStorableParam.StoreType.Physical;
        RegisterStringChooser(_linkJSON);

        var linkPopup = CreateScrollablePopup(_linkJSON, LeftSide);
        linkPopup.popupPanelHeight = 600f;

        if (!string.IsNullOrEmpty(_linkJSON.val))
            OnLinkChanged(_linkJSON.val);

        _activeJSON = new JSONStorableBool("Active", false);
        RegisterBool(_activeJSON);
        var activeToggle = CreateToggle(_activeJSON, LeftSide);

        _rotationLockJSON = new JSONStorableBool("Rotation Lock", false);
        RegisterBool(_rotationLockJSON);
        var rotationLockToggle = CreateToggle(_rotationLockJSON, LeftSide);

        _rotationLockNoRollJSON = new JSONStorableBool("No Roll", false);
        RegisterBool(_rotationLockNoRollJSON);
        var rotationLockNoRollToggle = CreateToggle(_rotationLockNoRollJSON, LeftSide);

        _positionLockJSON = new JSONStorableBool("Position Lock", true);
        RegisterBool(_positionLockJSON);
        var positionLockToggle = CreateToggle(_positionLockJSON, LeftSide);

        // Right Side

        _rotationSmoothingJSON = new JSONStorableFloat("Rotation Smoothing", 0.02f, 0f, 1f, true);
        RegisterFloat(_rotationSmoothingJSON);
        CreateSlider(_rotationSmoothingJSON, RightSide);

        _rotationOffsetXJSON = new JSONStorableFloat("Rotation X", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetXJSON);
        CreateSlider(_rotationOffsetXJSON, RightSide);

        _rotationOffsetYJSON = new JSONStorableFloat("Rotation Y", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetYJSON);
        CreateSlider(_rotationOffsetYJSON, RightSide);

        _rotationOffsetZJSON = new JSONStorableFloat("Rotation Z", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetZJSON);
        CreateSlider(_rotationOffsetZJSON, RightSide);

        _positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0.01f, 0f, 1f, true);
        RegisterFloat(_positionSmoothingJSON);
        CreateSlider(_positionSmoothingJSON, RightSide);

        _positionOffsetXJSON = new JSONStorableFloat("Position X", 0f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetXJSON);
        CreateSlider(_positionOffsetXJSON, RightSide).valueFormat = "F4";

        _positionOffsetYJSON = new JSONStorableFloat("Position Y", 0f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetYJSON);
        CreateSlider(_positionOffsetYJSON, RightSide).valueFormat = "F4";

        _positionOffsetZJSON = new JSONStorableFloat("Position Z", 0f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetZJSON);
        CreateSlider(_positionOffsetZJSON, RightSide).valueFormat = "F4";
    }

    private Slider CreateSlider(string label, float val, float max, bool constrained, string format)
    {
        var uiElement = CreateUIElement(manager.configurableSliderPrefab.transform, true);
        var dynamicSlider = uiElement.GetComponent<UIDynamicSlider>();
        dynamicSlider.Configure(label, -max, max, val, constrained, format, true, !constrained);
        return dynamicSlider.slider;
    }

    private void OnLinkChanged(string linkName)
    {
        if (containingAtom == null) throw new NullReferenceException("containingAtom");

        _link = containingAtom.linkableRigidbodies.FirstOrDefault(c => c.name == linkName);
        if (_link == null)
            SuperController.LogError("Controller does not exist: " + linkName);
    }

    public void Update()
    {
        try
        {
            var superController = SuperController.singleton;
            var navigationRig = superController.navigationRig;

            if (!_activeJSON.val || _link == null)
            {
                Restore();
                return;
            }

            var activatedThisFrame = false;
            if (!_active)
            {
                _previousRotation = navigationRig.rotation;
                _previousPosition = navigationRig.position;
                _previousPlayerHeight = superController.playerHeightAdjust;
                _active = true;
                activatedThisFrame = true;
            }

            var centerCameraTarget = superController.centerCameraTarget;
            var monitorCenterCamera = superController.MonitorCenterCamera;

            if (_rotationLockJSON.val || activatedThisFrame)
            {
                var navigationRigRotation = _link.transform.rotation;
                navigationRigRotation *= Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);
                if (_rotationLockNoRollJSON.val)
                {
                    navigationRigRotation.eulerAngles = new Vector3(navigationRigRotation.eulerAngles.x, navigationRigRotation.eulerAngles.y, 0f);
                }
                if (_rotationSmoothingJSON.val > 0 && !activatedThisFrame)
                {
                    navigationRigRotation = SmoothDamp(navigationRig.rotation, navigationRigRotation, ref _currentRotationVelocity, _rotationSmoothingJSON.val);
                }
                navigationRig.rotation = navigationRigRotation;
            }

            if (_positionLockJSON.val || activatedThisFrame)
            {
                var up = navigationRig.up;
                var targetPosition = _link.position + _link.transform.forward * _positionOffsetZJSON.val + _link.transform.right * _positionOffsetXJSON.val + _link.transform.up * _positionOffsetYJSON.val;
                var positionOffset = navigationRig.position + targetPosition - _possessor.autoSnapPoint.position;
                if (activatedThisFrame)
                {
                    // Adjust the player height so the user can adjust as needed
                    var playerHeightAdjustOffset = Vector3.Dot(positionOffset - navigationRig.position, up);
                    navigationRig.position = positionOffset + up * -playerHeightAdjustOffset;
                    superController.playerHeightAdjust += playerHeightAdjustOffset;
                }
                else
                {
                    // Lock down the position
                    if (_positionSmoothingJSON.val > 0 && !activatedThisFrame)
                    {
                        positionOffset = Vector3.SmoothDamp(navigationRig.position, positionOffset, ref _currentPositionVelocity, _positionSmoothingJSON.val);
                    }
                    navigationRig.position = positionOffset;
                }
            }
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update: " + e);
        }
    }

    public void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (_active)
        {
            SuperController.singleton.navigationRig.rotation = _previousRotation;
            SuperController.singleton.navigationRig.position = _previousPosition;
            SuperController.singleton.playerHeightAdjust = _previousPlayerHeight;
            _currentPositionVelocity = Vector3.zero;
            _active = false;
        }
    }

    // Source: https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
    public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion currentVelocity, float smoothTime)
    {
        // account for double-cover
        var Dot = Quaternion.Dot(current, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(current.x, target.x, ref currentVelocity.x, smoothTime),
            Mathf.SmoothDamp(current.y, target.y, ref currentVelocity.y, smoothTime),
            Mathf.SmoothDamp(current.z, target.z, ref currentVelocity.z, smoothTime),
            Mathf.SmoothDamp(current.w, target.w, ref currentVelocity.w, smoothTime)
        ).normalized;
        // compute deriv
        var dtInv = 1f / Time.deltaTime;
        currentVelocity.x = (Result.x - current.x) * dtInv;
        currentVelocity.y = (Result.y - current.y) * dtInv;
        currentVelocity.z = (Result.z - current.z) * dtInv;
        currentVelocity.w = (Result.w - current.w) * dtInv;
        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }
}
