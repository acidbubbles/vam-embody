using System;
using System.Collections;
using System.Linq;
using Interop;
using MeshVR;
using UnityEngine;

public class Passenger : MVRScript
{
    private const string _targetNone = "none";
    private Rigidbody _link;
    private Rigidbody _follower;
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
    private JSONStorableStringChooser _followerJSON;
    private JSONStorableBool _worldScaleEnabledJSON;
    private JSONStorableFloat _worldScaleJSON;
    private JSONStorableFloat _eyesToHeadDistanceJSON;
    private JSONStorableStringChooser _toggleKeyJSON;
    private KeyCode _toggleKey = KeyCode.None;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private float _previousWorldScale;
    private InteropProxy _interop;
    private bool _ready;
    private JSONStorableBool _lookAtJSON;
    private FreeControllerV3 _lookAt;

    public override void Init()
    {
        const bool leftSide = false;
        const bool rightSide = true;

        _interop = new InteropProxy(containingAtom);
        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        // Left Side

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Camera controller", (string val) => Reapply());
        RegisterStringChooser(_linkJSON);
        CreateFilterablePopup(_linkJSON).popupPanelHeight = 600f;

        _activeJSON = new JSONStorableBool("Active", false, val =>
        {
            if (!_ready) return;
            if (val)
                Activate();
            else
                Deactivate();
        });
        RegisterBool(_activeJSON);
        var activeToggle = CreateToggle(_activeJSON, leftSide);

        _lookAtJSON = new JSONStorableBool("Look At Eye Target", false, (bool val) => Reapply());
        if (containingAtom.type == "Person")
        {
            RegisterBool(_lookAtJSON);
            CreateToggle(_lookAtJSON);
            CreateButton("Select Eye Target").button.onClick.AddListener(() =>
            {
                var eyeTarget = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
                if (eyeTarget != null) SuperController.singleton.SelectController(eyeTarget);
            });
        }

        _rotationLockJSON = new JSONStorableBool("Rotation Lock", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_rotationLockJSON);
        var rotationLockToggle = CreateToggle(_rotationLockJSON, leftSide);

        _rotationLockNoRollJSON = new JSONStorableBool("No Roll", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_rotationLockNoRollJSON);
        var rotationLockNoRollToggle = CreateToggle(_rotationLockNoRollJSON, leftSide);

        _positionLockJSON = new JSONStorableBool("Position Lock", true, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_positionLockJSON);
        var positionLockToggle = CreateToggle(_positionLockJSON, leftSide);

        _worldScaleEnabledJSON = new JSONStorableBool("World Scale Enabled", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_worldScaleEnabledJSON);
        CreateToggle(_worldScaleEnabledJSON, leftSide);

        var controllers = containingAtom.freeControllers.Where(x => x.possessable && x.canGrabRotation).Select(x => x.name).ToList();
        controllers.Insert(0, _targetNone);
        _followerJSON = new JSONStorableStringChooser("Follow Controller", links, null, "Possess rotation of", (string val) => Reapply());
        RegisterStringChooser(_followerJSON);
        var followerPopup = CreateFilterablePopup(_followerJSON, leftSide);
        followerPopup.popupPanelHeight = 600f;

        var keys = Enum.GetNames(typeof(KeyCode)).ToList();
        _toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", keys, "None", "Toggle Key", val => { _toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        RegisterStringChooser(_toggleKeyJSON);
        var toggleKeyPopup = CreateFilterablePopup(_toggleKeyJSON, leftSide);
        toggleKeyPopup.popupPanelHeight = 600f;

        // Right Side

        _rotationSmoothingJSON = new JSONStorableFloat("Rotation Smoothing", 0f, 0f, 1f, true);
        RegisterFloat(_rotationSmoothingJSON);
        CreateSlider(_rotationSmoothingJSON, rightSide);

        _rotationOffsetXJSON = new JSONStorableFloat("Rotation X", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetXJSON);
        CreateSlider(_rotationOffsetXJSON, rightSide);

        _rotationOffsetYJSON = new JSONStorableFloat("Rotation Y", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetYJSON);
        CreateSlider(_rotationOffsetYJSON, rightSide);

        _rotationOffsetZJSON = new JSONStorableFloat("Rotation Z", 0f, -180, 180, true, true);
        RegisterFloat(_rotationOffsetZJSON);
        CreateSlider(_rotationOffsetZJSON, rightSide);

        _positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0f, 0f, 1f, true);
        RegisterFloat(_positionSmoothingJSON);
        CreateSlider(_positionSmoothingJSON, rightSide);

        _positionOffsetXJSON = new JSONStorableFloat("Position X", 0f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetXJSON);
        CreateSlider(_positionOffsetXJSON, rightSide).valueFormat = "F4";

        _positionOffsetYJSON = new JSONStorableFloat("Position Y", 0.06f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetYJSON);
        CreateSlider(_positionOffsetYJSON, rightSide).valueFormat = "F4";

        _positionOffsetZJSON = new JSONStorableFloat("Position Z", 0f, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetZJSON);
        CreateSlider(_positionOffsetZJSON, rightSide).valueFormat = "F4";

        _worldScaleJSON = new JSONStorableFloat("World Scale", 1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0.1f, 10f);
        RegisterFloat(_worldScaleJSON);
        CreateSlider(_worldScaleJSON, rightSide);

        _eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0.1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0f, 0.2f, false);
        RegisterFloat(_eyesToHeadDistanceJSON);
        CreateSlider(_eyesToHeadDistanceJSON, rightSide);

        // Deferred
        SuperController.singleton.StartCoroutine(InitDeferred());
    }

    private void Reapply()
    {
        if (_activeJSON?.val != true) return;
        _activeJSON.val = false;
        _activeJSON.val = true;
    }

    private IEnumerator InitDeferred()
    {
        yield return new WaitForEndOfFrame();
        _interop.Connect();
        _ready = true;
        if (_activeJSON.val)
            Activate();
    }

    public void OnDisable()
    {
        if (_activeJSON.val) _activeJSON.val = false;
    }

    private void Activate()
    {
        try
        {
            _link = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == _linkJSON.val);
            if (_lookAtJSON.val)
                _lookAt = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
            if (!string.IsNullOrEmpty(_followerJSON.val))
                _follower = containingAtom.linkableRigidbodies.FirstOrDefault(rb => rb.name == _followerJSON.val);

            if (!CanActivate() || !IsValid())
            {
                _activeJSON.valNoCallback = false;
                return;
            }

            if (_link.name == "head")
            {
                if (_interop.improvedPoV?.possessedOnlyJSON != null)
                    _interop.improvedPoV.possessedOnlyJSON.val = false;
            }

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

            GlobalSceneOptions.singleton.disableNavigation = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to activate Passenger.\n{exc}");
            _activeJSON.valNoCallback = false;
            Deactivate();
        }
    }

    private bool CanActivate()
    {
        if (_link == null)
        {
            SuperController.LogError("Embody: Could not find the specified link.");
            return false;
        }

        var linkController = containingAtom.GetStorableByID(_link.name.EndsWith("Control") ? _link.name : $"{_link.name}Control") as FreeControllerV3;
        if (linkController != null && linkController.possessed)
        {
            SuperController.LogError(
                $"Embody: Cannot activate Passenger while the target rigidbody {_link.name} is being possessed. Use the 'Active' checkbox or trigger instead of using built-in Virt-A-Mate possession.");
            return false;
        }

        return true;
    }

    private bool IsValid()
    {
        if (_preferences.useHeadCollider)
        {
            SuperController.LogError("Embody: Do not enable the head collider with Passenger, they do not work together!");
            return false;
        }

        return true;
    }

    private void ApplyWorldScale()
    {
        if (!_worldScaleEnabledJSON.val) return;

        _previousWorldScale = SuperController.singleton.worldScale;
        SuperController.singleton.worldScale = _worldScaleJSON.val;
    }

    private void Deactivate()
    {
        GlobalSceneOptions.singleton.disableNavigation = false;

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
            {
                if (_interop.improvedPoV?.possessedOnlyJSON != null)
                    _interop.improvedPoV.possessedOnlyJSON.val = true;
            }
        }

        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        _startRotationOffset = Quaternion.identity;

        _link = null;
        _lookAt = null;
    }

    public void Update()
    {
        try
        {
            if (!_activeJSON.val)
            {
                if (_toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
                {
                    _activeJSON.val = true;
                    return;
                }

                return;
            }

            if (!IsValid())
            {
                _activeJSON.val = false;
                return;
            }

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
            SuperController.LogError($"Embody: Failed to update.\n{e}");
            _activeJSON.val = false;
        }
    }

    private void UpdatePosition(Transform navigationRig, float positionSmoothing)
    {
        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _link.transform;

        var cameraDelta = centerTargetTransform.position
                          - navigationRigTransform.position
                          - centerTargetTransform.rotation * new Vector3(0, 0, _eyesToHeadDistanceJSON.val);
        var resultPosition = linkTransform.position
                             - cameraDelta
                             + linkTransform.rotation * new Vector3(_positionOffsetXJSON.val, _positionOffsetYJSON.val, _positionOffsetZJSON.val);

        if (positionSmoothing > 0)
            resultPosition = Vector3.SmoothDamp(navigationRig.position, resultPosition, ref _currentPositionVelocity, positionSmoothing, Mathf.Infinity, Time.smoothDeltaTime);

        navigationRigTransform.position = resultPosition;
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
            navigationRigRotation = navigationRig.rotation.SmoothDamp(navigationRigRotation, ref _currentRotationVelocity, rotationSmoothing);

        navigationRig.rotation = navigationRigRotation;
    }
}
