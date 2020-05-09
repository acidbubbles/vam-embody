using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Passenger
/// By Acidbubbles
/// Your eyes follow the model's eyes, not the other way around
/// Source: https://github.com/acidbubbles/vam-passenger
/// </summary>
public class Passenger : MVRScript
{
    private const string TargetNone = "none";
    private Rigidbody _link;
    private Rigidbody _follower;
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
    private JSONStorableStringChooser _followerJSON;
    private JSONStorableBool _worldScaleEnabledJSON;
    private JSONStorableFloat _worldScaleJSON;
    private JSONStorableFloat _eyesToHeadDistanceJSON;
    private JSONStorableStringChooser _toggleKeyJSON;
    private KeyCode _toggleKey = KeyCode.None;
    private Vector3 _previousPosition;
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
        links.Insert(0, "none");
        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Camera controller", OnLinkChanged)
        {
            storeType = JSONStorableParam.StoreType.Physical
        };
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

        _worldScaleEnabledJSON = new JSONStorableBool("World Scale Enabled", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_worldScaleEnabledJSON);
        CreateToggle(_worldScaleEnabledJSON, LeftSide);

        var controllers = containingAtom.freeControllers.Where(x => x.possessable && x.canGrabRotation).Select(x => x.name).ToList();
        controllers.Insert(0, TargetNone);
        _followerJSON = new JSONStorableStringChooser("Follow Controller", links, null, "Possess rotation of", OnFollowChanged);
        RegisterStringChooser(_followerJSON);
        var followerPopup = CreateScrollablePopup(_followerJSON, LeftSide);
        followerPopup.popupPanelHeight = 600f;
        if (!string.IsNullOrEmpty(_followerJSON.val))
            OnLinkChanged(_followerJSON.val);

        var keys = Enum.GetNames(typeof(KeyCode)).ToList();
        _toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", keys, "None", "Toggle Key", new JSONStorableStringChooser.SetStringCallback(v => ApplyToggleKey(v)));
        RegisterStringChooser(_toggleKeyJSON);
        var toggleKeyPopup = CreateScrollablePopup(_toggleKeyJSON, LeftSide);
        toggleKeyPopup.popupPanelHeight = 600f;
        ApplyToggleKey(_toggleKeyJSON.val);

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

        _worldScaleJSON = new JSONStorableFloat("World Scale", 1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0.1f, 10f);
        RegisterFloat(_worldScaleJSON);
        CreateSlider(_worldScaleJSON, RightSide);

        _eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0.1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0f, 0.2f, false);
        RegisterFloat(_eyesToHeadDistanceJSON);
        CreateSlider(_eyesToHeadDistanceJSON, RightSide);
    }

    private void ApplyToggleKey(string val)
    {
        _toggleKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
    }

    private void OnLinkChanged(string linkName)
    {
        OnTargetControllerChanged(ref _link, linkName);
    }

    private void OnFollowChanged(string linkName)
    {
        OnTargetControllerChanged(ref _follower, linkName);
    }

    private void OnTargetControllerChanged(ref Rigidbody target, string linkName)
    {
        if (containingAtom == null) throw new NullReferenceException("containingAtom");

        if (_activeJSON != null)
        {
            Deactivate();
            _activeJSON.val = false;
        }

        if (linkName == TargetNone)
        {
            target = null;
            return;
        }

        target = containingAtom.linkableRigidbodies.FirstOrDefault(c => c.name == linkName);
        if (target == null)
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

        var rigidBody = _link.GetComponent<Rigidbody>();
        _previousInterpolation = rigidBody.interpolation;
        rigidBody.interpolation = RigidbodyInterpolation.Interpolate;

        var offsetStartRotation = !superController.MonitorRig.gameObject.activeSelf;
        if (offsetStartRotation)
            _startRotationOffset = Quaternion.Euler(0, navigationRig.eulerAngles.y - _possessor.transform.eulerAngles.y, 0f);

        ApplyWorldScale();
        UpdateRotation(navigationRig, 0);
        UpdatePosition(navigationRig, 0);

        _active = true;
    }

    private void ApplyWorldScale()
    {
        if (!_worldScaleEnabledJSON.val) return;

        _previousWorldScale = SuperController.singleton.worldScale;
        SuperController.singleton.worldScale = _worldScaleJSON.val;
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
            if (!_active)
            {
                if (_toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
                {
                    _activeJSON.val = true;
                    return;
                }

                return;
            }

            if (!HealthCheck()) return;

            if (Input.GetKeyDown(KeyCode.Escape) || _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
            {
                _activeJSON.val = false;
                return;
            }

            var navigationRig = SuperController.singleton.navigationRig;

            if (_follower != null)
                PossessRotation();
            else if (_rotationLockJSON.val)
                UpdateRotation(navigationRig, _rotationSmoothingJSON.val);

            if (_positionLockJSON.val)
                UpdatePosition(navigationRig, _positionSmoothingJSON.val);
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

    private void UpdatePosition(Transform navigationRig, float positionSmoothing)
    {
        var cameraDelta = CameraTarget.centerTarget.transform.position - navigationRig.transform.position - CameraTarget.centerTarget.transform.rotation * new Vector3(0, 0, _eyesToHeadDistanceJSON.val);
        var resultPosition = _link.transform.position - cameraDelta + _link.transform.rotation * new Vector3(_positionOffsetXJSON.val, _positionOffsetYJSON.val, _positionOffsetZJSON.val);

        if (positionSmoothing > 0)
            resultPosition = Vector3.SmoothDamp(navigationRig.position, resultPosition, ref _currentPositionVelocity, positionSmoothing, Mathf.Infinity, Time.smoothDeltaTime);

        navigationRig.transform.position = resultPosition;
    }

    private void PossessRotation()
    {
        _follower.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation
         * Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);
    }

    private void UpdateRotation(Transform navigationRig, float rotationSmoothing)
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
