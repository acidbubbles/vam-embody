#define VAM_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Passenger Version 0.0.0
/// By Acidbubbles
/// Your eyes follow the model's eyes, not the other way around
/// Source: https://github.com/acidbubbles/vam-passenger
/// </summary>
public class Passenger : MVRScript
{
    private Rigidbody _link;
    private Possessor _possessor;
    private FreeControllerV3 _headControl;
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
    private JSONStorableBool _worldScaleEnabled;
    private JSONStorableFloat _worldScale;
    private Vector3 _previousPosition;
    private float _previousPlayerHeight;
    private Quaternion _previousRotation;
    private bool _active;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private float _previousWorldScale;

    public override void Init()
    {
        try
        {
            _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
            _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
            _headControl = (FreeControllerV3)containingAtom.GetStorableByID("headControl");

            InitControls();

            if (_activeJSON.val)
                Activate();
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

        _activeJSON = new JSONStorableBool("Active", false, val =>
        {
            if (val)
                Activate();
            else
                Deactivate();
        });
        RegisterBool(_activeJSON);
        var activeToggle = CreateToggle(_activeJSON, LeftSide);

        _rotationLockJSON = new JSONStorableBool("Rotation Lock", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_rotationLockJSON);
        var rotationLockToggle = CreateToggle(_rotationLockJSON, LeftSide);

        _rotationLockNoRollJSON = new JSONStorableBool("No Roll", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_rotationLockNoRollJSON);
        var rotationLockNoRollToggle = CreateToggle(_rotationLockNoRollJSON, LeftSide);

        _positionLockJSON = new JSONStorableBool("Position Lock", true, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_positionLockJSON);
        var positionLockToggle = CreateToggle(_positionLockJSON, LeftSide);

        _worldScaleEnabled = new JSONStorableBool("World Scale Enabled", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_worldScaleEnabled);
        CreateToggle(_worldScaleEnabled, LeftSide);

        // Right Side

        _rotationSmoothingJSON = new JSONStorableFloat("Rotation Smoothing", 0f, 0f, 1f, true);
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

        _positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0f, 0f, 1f, true);
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

        _worldScale = new JSONStorableFloat("World Scale", 1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0.1f, 10f);
        RegisterFloat(_worldScale);
        CreateSlider(_worldScale, RightSide);
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

        if (_activeJSON != null)
        {
            Deactivate();
            _activeJSON.val = false;
        }

        _link = containingAtom.linkableRigidbodies.FirstOrDefault(c => c.name == linkName);
        if (_link == null)
            SuperController.LogError("Controller does not exist: " + linkName);
    }

    private void Activate()
    {
        if (_active) return;
        if (!HealthCheck()) return;

        if (_link.name == "head")
            GetImprovedPoVOnlyWhenOnlyWhenPossessedJSON()?.SetVal(false);

        var superController = SuperController.singleton;
        var navigationRig = superController.navigationRig;

        _previousRotation = navigationRig.rotation;
        _previousPosition = navigationRig.position;
        _previousPlayerHeight = superController.playerHeightAdjust;

        var rigidBody = _link.GetComponent<Rigidbody>();
        _previousInterpolation = rigidBody.interpolation;
        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;

        var offsetStartRotation = !superController.MonitorRig.gameObject.activeSelf;
        if (offsetStartRotation)
            _startRotationOffset = Quaternion.Euler(0, navigationRig.eulerAngles.y - _possessor.transform.eulerAngles.y, 0f);

        ApplyWorldScale();
        ApplyRotation(navigationRig, 0);
        MoveToStartingPosition(navigationRig, GetTargetPosition(navigationRig));

        _active = true;
    }

    private void ApplyWorldScale()
    {
        if (!_worldScaleEnabled.val) return;

        _previousWorldScale = SuperController.singleton.worldScale;
        SuperController.singleton.worldScale = _worldScale.val;
    }

    private void Deactivate()
    {
        if (!_active) return;

        if (_previousWorldScale > 0f)
        {
            SuperController.singleton.worldScale = _previousWorldScale;
            _previousWorldScale = 0f;
        }

        SuperController.singleton.navigationRig.rotation = _previousRotation;
        SuperController.singleton.navigationRig.position = _previousPosition;
        SuperController.singleton.playerHeightAdjust = _previousPlayerHeight;

        if (_link != null)
        {
            var rigidBody = _link.GetComponent<Rigidbody>();
            rigidBody.interpolation = _previousInterpolation;

            if (_link.name == "head")
                GetImprovedPoVOnlyWhenOnlyWhenPossessedJSON()?.SetVal(true);
        }

        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        _startRotationOffset = Quaternion.identity;

        _active = false;
    }

    private void Reapply()
    {
        if (!_active) return;

        Deactivate();
        Activate();
    }

    public void Update()
    {
        try
        {
            if (!_active) return;
            if (!HealthCheck()) return;

            var navigationRig = SuperController.singleton.navigationRig;

            if (_rotationLockJSON.val)
                ApplyRotation(navigationRig, _rotationSmoothingJSON.val);

            if (_positionLockJSON.val)
                ApplyPosition(navigationRig, GetTargetPosition(navigationRig));
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update: " + e);
        }
    }

    private void AbortActivation(string message)
    {
        SuperController.LogError(message);
        _activeJSON.val = false;
        Deactivate();
    }

    private Vector3 GetTargetPosition(Transform navigationRig)
    {
        var targetPosition = _link.position + _link.transform.forward * _positionOffsetZJSON.val + _link.transform.right * _positionOffsetXJSON.val + _link.transform.up * _positionOffsetYJSON.val;
        var positionOffset = navigationRig.position + targetPosition - _possessor.autoSnapPoint.position;
        return positionOffset;
    }

    private void ApplyPosition(Transform navigationRig, Vector3 targetPosition)
    {
        if (_positionSmoothingJSON.val > 0)
            targetPosition = Vector3.SmoothDamp(navigationRig.position, targetPosition, ref _currentPositionVelocity, _positionSmoothingJSON.val, Mathf.Infinity, Time.smoothDeltaTime);

        navigationRig.position = targetPosition;
    }

    private static void MoveToStartingPosition(Transform navigationRig, Vector3 targetPosition)
    {
        // Adjust the player height so the user can adjust as needed
        var up = navigationRig.up;
        var playerHeightAdjustOffset = Vector3.Dot(targetPosition - navigationRig.position, up);
        navigationRig.position = targetPosition + up * -playerHeightAdjustOffset;
        SuperController.singleton.playerHeightAdjust += playerHeightAdjustOffset;
    }

    private void ApplyRotation(Transform navigationRig, float rotationSmoothing)
    {
        var navigationRigRotation = _link.transform.rotation;

        if (_rotationLockNoRollJSON.val)
            navigationRigRotation.SetLookRotation(navigationRigRotation * Vector3.forward, Vector3.up);

        // TODO? Necessary?
        if (_startRotationOffset == Quaternion.identity)
            navigationRigRotation *= _startRotationOffset;

        navigationRigRotation *= Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);

        if (rotationSmoothing > 0)
            navigationRigRotation = SmoothDamp(navigationRig.rotation, navigationRigRotation, ref _currentRotationVelocity, rotationSmoothing);

        navigationRig.rotation = navigationRigRotation;
    }

    private bool HealthCheck()
    {
        if (_link == null)
        {
            AbortActivation("There is no link or the current link does not exist");
            return false;
        }

        if (_headControl != null && _headControl.possessed)
        {
            AbortActivation("Virt-A-Mate possession and Passenger don't work together! Use Passenger's Active checkbox instead");
            return false;
        }

        if (_preferences.useHeadCollider)
        {
            AbortActivation("Do not enable the head collider with Passenger, they do not work together!");
            return false;
        }

        return true;
    }

    public void OnDisable()
    {
        try
        {
            Deactivate();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to disable: " + e);
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
        var dtInv = 1f / Time.smoothDeltaTime;
        currentVelocity.x = (Result.x - current.x) * dtInv;
        currentVelocity.y = (Result.y - current.y) * dtInv;
        currentVelocity.z = (Result.z - current.z) * dtInv;
        currentVelocity.w = (Result.w - current.w) * dtInv;
        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }

    private JSONStorableBool GetImprovedPoVOnlyWhenOnlyWhenPossessedJSON()
    {
        if (containingAtom == null) return null;
        var improvedPoVStorableID = containingAtom.GetStorableIDs().FirstOrDefault(id => id.EndsWith("ImprovedPoV"));
        if (improvedPoVStorableID == null) return null;
        var improvedPoVStorable = containingAtom.GetStorableByID(improvedPoVStorableID);
        return improvedPoVStorable?.GetBoolJSONParam("Activate only when possessed");
    }
}
