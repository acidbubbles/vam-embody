using System;
using System.Linq;
using Interop;
using MeshVR;
using UnityEngine;

public class Passenger : MVRScript, IPassenger
{
    private const string _targetNone = "none";

    private Rigidbody _link;
    private Rigidbody _follower;
    private Possessor _possessor;
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
    private JSONStorableFloat _eyesToHeadDistanceJSON;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private InteropProxy _interop;
    private JSONStorableBool _lookAtJSON;
    private FreeControllerV3 _lookAt;

    public override void Init()
    {
        const bool leftSide = false;
        const bool rightSide = true;

        _interop = new InteropProxy(this, containingAtom);
        _interop.Init();
        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        // Left Side

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Camera controller", (string val) => Reapply());
        RegisterStringChooser(_linkJSON);
        CreateFilterablePopup(_linkJSON).popupPanelHeight = 600f;

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

        var controllers = containingAtom.freeControllers.Where(x => x.possessable && x.canGrabRotation).Select(x => x.name).ToList();
        controllers.Insert(0, _targetNone);
        _followerJSON = new JSONStorableStringChooser("Follow Controller", links, null, "Possess rotation of", (string val) => Reapply());
        RegisterStringChooser(_followerJSON);
        var followerPopup = CreateFilterablePopup(_followerJSON, leftSide);
        followerPopup.popupPanelHeight = 600f;

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

        _eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0.1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0f, 0.2f, false);
        RegisterFloat(_eyesToHeadDistanceJSON);
        CreateSlider(_eyesToHeadDistanceJSON, rightSide);
    }

    private void Reapply()
    {
        if (enabledJSON?.val != true) return;
        enabledJSON.val = false;
        enabledJSON.val = true;
    }

    public void OnEnable()
    {
        if (_interop?.ready != true) return;

        try
        {
            _link = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == _linkJSON.val);
            if (_lookAtJSON.val)
                _lookAt = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
            if (!string.IsNullOrEmpty(_followerJSON.val))
                _follower = containingAtom.linkableRigidbodies.FirstOrDefault(rb => rb.name == _followerJSON.val);

            if (!CanActivate() || !IsValid())
            {
                enabledJSON.val = false;
                return;
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

            UpdateRotation(navigationRig, 0);
            UpdatePosition(navigationRig, 0);

            GlobalSceneOptions.singleton.disableNavigation = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to activate Passenger.\n{exc}");
            enabledJSON.val = false;
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

    public void OnDisable()
    {
        if (_interop?.ready != true) return;

        GlobalSceneOptions.singleton.disableNavigation = false;

        SuperController.singleton.navigationRig.rotation = _previousRotation;
        SuperController.singleton.navigationRig.position = _previousPosition;

        if (_link != null)
        {
            var rigidBody = _link.GetComponent<Rigidbody>();
            rigidBody.interpolation = _previousInterpolation;
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
            if (!IsValid())
            {
                enabledJSON.val = false;
                return;
            }

            var navigationRig = SuperController.singleton.navigationRig;

            // if (_follower != null)
            //     PossessRotation();
            // else if (_rotationLockJSON.val)
            //     UpdateRotation(navigationRig, _rotationSmoothingJSON.val);
            //
            // if (_positionLockJSON.val)
            //     UpdatePosition(navigationRig, _positionSmoothingJSON.val);

            UpdateNavigationRig();
        }
        catch (Exception e)
        {
            SuperController.LogError($"Embody: Failed to update.\n{e}");
            enabledJSON.val = false;
        }
    }

    private void UpdateNavigationRig()
    {
        // Context
        var positionSmoothing = _positionSmoothingJSON.val;
        var rotationSmoothing = _rotationSmoothingJSON.val;
        var navigationRig = SuperController.singleton.navigationRig;
        var positionOffset = new Vector3(_positionOffsetXJSON.val, _positionOffsetYJSON.val, _positionOffsetZJSON.val);
        var eyesToHeadOffset = new Vector3(0, 0, _eyesToHeadDistanceJSON.val);
        var rotationOffset = Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);

        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _link.transform;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation;

        if(!ReferenceEquals(_lookAt, null))
            rotation.SetLookRotation(_lookAt.transform.position - linkTransform.position, linkTransform.up);

        rotation *= rotationOffset;

        if (_rotationLockNoRollJSON.val)
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);

        position += rotation * positionOffset;

        // Move navigation rig

        var cameraDelta = centerTargetTransform.position
                          - navigationRigTransform.position
                          - centerTargetTransform.rotation * eyesToHeadOffset;
        var navigationRigPosition = position - cameraDelta;

        var navigationRigRotation = rotation;
        if (_startRotationOffset == Quaternion.identity)
            navigationRigRotation *= _startRotationOffset;

        // TODO? Necessary?
        if (_startRotationOffset == Quaternion.identity)
            navigationRigRotation *= _startRotationOffset;

        if (positionSmoothing > 0)
            navigationRigPosition = Vector3.SmoothDamp(navigationRig.position, navigationRigPosition, ref _currentPositionVelocity, positionSmoothing, Mathf.Infinity, Time.smoothDeltaTime);

        if (rotationSmoothing > 0)
            navigationRigRotation = navigationRig.rotation.SmoothDamp(navigationRigRotation, ref _currentRotationVelocity, rotationSmoothing);

        navigationRigTransform.rotation = navigationRigRotation;
        navigationRigTransform.position = navigationRigPosition;

        // TODO: Re-position the main menu and the hud anchors (ovr) if that's possible
        // TODO: If we can move the navigation rig during fixed update (see ovr) we could stabilize before vam does raycasting & positionning
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

        if(!ReferenceEquals(_lookAt, null))
            navigationRigRotation.SetLookRotation(_lookAt.transform.position - _link.position, _link.transform.up);
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
