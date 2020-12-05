using System;
using System.Collections.Generic;
using System.Linq;
using Interop;
using MeshVR;
using UnityEngine;

public class Passenger : MVRScript, IPassenger
{
    private const string _targetNone = "none";

    private Rigidbody _link;
    private Rigidbody _possessedLink;
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
    private JSONStorableStringChooser _possessRotationLink;
    private JSONStorableFloat _eyesToHeadDistanceJSON;
    private Vector3 _positionOffset;
    private Quaternion _rotationOffset;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private InteropProxy _interop;
    private JSONStorableBool _lookAtJSON;
    private JSONStorableFloat _lookAtWeightJSON;
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
        _linkJSON = new JSONStorableStringChooser("Link", links, defaultLink, "Link to", (string val) => Reapply());
        RegisterStringChooser(_linkJSON);
        CreateFilterablePopup(_linkJSON).popupPanelHeight = 600f;

        _lookAtJSON = new JSONStorableBool("LookAtEyeTarget", false, (bool val) => Reapply());
        if (containingAtom.type == "Person")
        {
            RegisterBool(_lookAtJSON);
            CreateToggle(_lookAtJSON).label = "Look at eye target";
            CreateButton("Select eye target").button.onClick.AddListener(() =>
            {
                var eyeTarget = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
                if (eyeTarget != null) SuperController.singleton.SelectController(eyeTarget);
            });
        }

        _lookAtWeightJSON = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);
        RegisterFloat(_lookAtWeightJSON);
        CreateSlider(_lookAtWeightJSON).label = "Look at weight";

        _positionLockJSON = new JSONStorableBool("ControlPosition", true, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_positionLockJSON);
         CreateToggle(_positionLockJSON, leftSide).label = "Control camera position";

        _rotationLockJSON = new JSONStorableBool("ControlRotation", false, new JSONStorableBool.SetBoolCallback(v =>
        {
            _possessRotationLink.valNoCallback = null;
            Reapply();
        }));
        RegisterBool(_rotationLockJSON);
        CreateToggle(_rotationLockJSON, leftSide).label = "Control camera rotation";

        _rotationLockNoRollJSON = new JSONStorableBool("NoRoll", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));
        RegisterBool(_rotationLockNoRollJSON);
        CreateToggle(_rotationLockNoRollJSON, leftSide).label = "Prevent camera roll";

        Func<List<string>> getPossessRotationChoices = () =>
        {
            var controllers = containingAtom.freeControllers.Where(x => x.possessable && x.canGrabRotation).Select(x => x.name).ToList();
            controllers.Insert(0, _targetNone);
            return controllers;
        };
        _possessRotationLink = new JSONStorableStringChooser("PossessRotationLink", getPossessRotationChoices(), _targetNone, "Possess rotation of", (string val) =>
        {
            _rotationLockJSON.valNoCallback = false;
            Reapply();
        });
        RegisterStringChooser(_possessRotationLink);
        var followerPopup = CreateFilterablePopup(_possessRotationLink, leftSide);
        followerPopup.popupPanelHeight = 600f;
        followerPopup.popup.onOpenPopupHandlers += () =>
        {
            _possessRotationLink.choices = getPossessRotationChoices();
        };

        // Right Side

        _rotationSmoothingJSON = new JSONStorableFloat("Rotation Smoothing", 0f, 0f, 1f, true);
        RegisterFloat(_rotationSmoothingJSON);
        CreateSlider(_rotationSmoothingJSON, rightSide);

        _rotationOffsetXJSON = new JSONStorableFloat("Rotation X", 0f, SyncRotationOffset, -180, 180, true, true);
        RegisterFloat(_rotationOffsetXJSON);
        CreateSlider(_rotationOffsetXJSON, rightSide);

        _rotationOffsetYJSON = new JSONStorableFloat("Rotation Y", 0f, SyncRotationOffset, -180, 180, true, true);
        RegisterFloat(_rotationOffsetYJSON);
        CreateSlider(_rotationOffsetYJSON, rightSide);

        _rotationOffsetZJSON = new JSONStorableFloat("Rotation Z", 0f, SyncRotationOffset, -180, 180, true, true);
        RegisterFloat(_rotationOffsetZJSON);
        CreateSlider(_rotationOffsetZJSON, rightSide);

        _positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0f, 0f, 1f, true);
        RegisterFloat(_positionSmoothingJSON);
        CreateSlider(_positionSmoothingJSON, rightSide);

        _positionOffsetXJSON = new JSONStorableFloat("Position X", 0f,  SyncPositionOffset, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetXJSON);
        CreateSlider(_positionOffsetXJSON, rightSide).valueFormat = "F4";

        _positionOffsetYJSON = new JSONStorableFloat("Position Y", 0.08f, SyncPositionOffset, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetYJSON);
        CreateSlider(_positionOffsetYJSON, rightSide).valueFormat = "F4";

        _positionOffsetZJSON = new JSONStorableFloat("Position Z", 0f, SyncPositionOffset, -2f, 2f, false, true);
        RegisterFloat(_positionOffsetZJSON);
        CreateSlider(_positionOffsetZJSON, rightSide).valueFormat = "F4";

        _eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0.1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0f, 0.2f, false);
        RegisterFloat(_eyesToHeadDistanceJSON);
        CreateSlider(_eyesToHeadDistanceJSON, rightSide);

        SyncPositionOffset(0);
        SyncRotationOffset(0);
    }

    private void SyncRotationOffset(float _)
    {
        _rotationOffset = Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);
    }

    private void SyncPositionOffset(float _)
    {
        _positionOffset = new Vector3(_positionOffsetXJSON.val, _positionOffsetYJSON.val, _positionOffsetZJSON.val);
    }

    private void Reapply()
    {
        if (enabledJSON?.val != true) return;
        enabledJSON.val = false;
        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        enabledJSON.val = true;
        UpdateNavigationRig(true);
    }

    public void OnEnable()
    {
        if (_interop?.ready != true) return;

        try
        {
            _link = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == _linkJSON.val);
            if (_lookAtJSON.val)
                _lookAt = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
            if (_possessRotationLink.val != _targetNone)
                _possessedLink = containingAtom.linkableRigidbodies.FirstOrDefault(rb => rb.name == _possessRotationLink.val);

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
            rigidBody.interpolation = RigidbodyInterpolation.Extrapolate;

            var offsetStartRotation = !superController.MonitorRig.gameObject.activeSelf;
            if (offsetStartRotation)
                _startRotationOffset = Quaternion.Euler(0, navigationRig.eulerAngles.y - _possessor.transform.eulerAngles.y, 0f);

            GlobalSceneOptions.singleton.disableNavigation = true;

            UpdateNavigationRig(true);
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

            UpdateNavigationRig(false);
        }
        catch (Exception e)
        {
            SuperController.LogError($"Embody: Failed to apply Passenger.\n{e}");
            enabledJSON.val = false;
        }
    }

    private void UpdateNavigationRig(bool force)
    {
        // Context
        var positionSmoothing = _positionSmoothingJSON.val;
        var rotationSmoothing = _rotationSmoothingJSON.val;
        var navigationRig = SuperController.singleton.navigationRig;
        var eyesToHeadOffset = new Vector3(0, 0, _eyesToHeadDistanceJSON.val);

        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _link.transform;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation;

        if (!ReferenceEquals(_lookAt, null) && _lookAtWeightJSON.val > 0)
        {
            var lookAtRotation = Quaternion.LookRotation(_lookAt.transform.position - linkTransform.position, linkTransform.up);
            rotation = Quaternion.Slerp(rotation, lookAtRotation, _lookAtWeightJSON.val);
        }

        rotation *= _rotationOffset;

        if (_rotationLockNoRollJSON.val)
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);

        position += rotation * _positionOffset;

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

        if (!force)
        {
            if (positionSmoothing > 0)
                navigationRigPosition = Vector3.SmoothDamp(navigationRig.position, navigationRigPosition, ref _currentPositionVelocity, positionSmoothing, Mathf.Infinity, Time.smoothDeltaTime);

            if (rotationSmoothing > 0)
                navigationRigRotation = navigationRig.rotation.SmoothDamp(navigationRigRotation, ref _currentRotationVelocity, rotationSmoothing);
        }

        if (force || _rotationLockJSON.val)
        {
            if (!ReferenceEquals(_possessedLink, null))
                _possessedLink.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation * _rotationOffset;
            else
                navigationRigTransform.rotation = navigationRigRotation;
        }

        if (force || _positionLockJSON.val)
        {
            navigationRigTransform.position = navigationRigPosition;
        }

        // TODO: Re-position the main menu and the hud anchors (ovr) if that's possible
        // TODO: If we can move the navigation rig during fixed update (see ovr) we could stabilize before vam does raycasting & positionning
    }
}
