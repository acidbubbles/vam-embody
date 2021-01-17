using System;
using System.Collections.Generic;
using System.Linq;
using MeshVR;
using SimpleJSON;
using UnityEngine;

public interface IPassengerModule : IEmbodyModule
{
    JSONStorableStringChooser linkJSON { get; }
    JSONStorableStringChooser possessRotationLink { get; }
    JSONStorableFloat eyesToHeadDistanceJSON { get; }
    JSONStorableBool lookAtJSON { get; }
    JSONStorableFloat lookAtWeightJSON { get; }
    JSONStorableBool rotationLockJSON { get; }
    JSONStorableBool rotationLockNoRollJSON { get; }
    JSONStorableFloat rotationSmoothingJSON { get; }
    JSONStorableFloat rotationOffsetXjson { get; }
    JSONStorableFloat rotationOffsetYjson { get; }
    JSONStorableFloat rotationOffsetZjson { get; }
    JSONStorableBool positionLockJSON { get; }
    JSONStorableFloat positionSmoothingJSON { get; }
    JSONStorableFloat positionOffsetXjson { get; }
    JSONStorableFloat positionOffsetYjson { get; }
    JSONStorableFloat positionOffsetZjson { get; }
}

public class PassengerModule : EmbodyModuleBase, IPassengerModule
{
    public const string Label = "Passenger";

    private const string _targetNone = "none";

    public override string storeId => "Passenger";
    public override string label => Label;

    public JSONStorableStringChooser linkJSON { get; set; }
    public JSONStorableStringChooser possessRotationLink { get; set; }
    public JSONStorableFloat eyesToHeadDistanceJSON { get; set; }
    public JSONStorableBool lookAtJSON { get; set; }
    public JSONStorableFloat lookAtWeightJSON { get; set; }
    public JSONStorableBool rotationLockJSON { get; set; }
    public JSONStorableBool rotationLockNoRollJSON { get; set; }
    public JSONStorableFloat rotationSmoothingJSON { get; set; }
    public JSONStorableFloat rotationOffsetXjson { get; set; }
    public JSONStorableFloat rotationOffsetYjson { get; set; }
    public JSONStorableFloat rotationOffsetZjson { get; set; }
    public JSONStorableBool positionLockJSON { get; set; }
    public JSONStorableFloat positionSmoothingJSON { get; set; }
    public JSONStorableFloat positionOffsetXjson { get; set; }
    public JSONStorableFloat positionOffsetYjson { get; set; }
    public JSONStorableFloat positionOffsetZjson { get; set; }

    private Rigidbody _link;
    private Rigidbody _possessedLink;
    private Possessor _possessor;
    private Vector3 _positionOffset;
    private Quaternion _rotationOffset;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private FreeControllerV3 _lookAt;

    public override void Awake()
    {
        base.Awake();

        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();
        linkJSON = new JSONStorableStringChooser("Link", links, defaultLink, "Link to", (string val) => Reapply());

        lookAtJSON = new JSONStorableBool("LookAtEyeTarget", false, (bool val) => Reapply());

        lookAtWeightJSON = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);

        positionLockJSON = new JSONStorableBool("ControlPosition", true, new JSONStorableBool.SetBoolCallback(v => Reapply()));

        rotationLockJSON = new JSONStorableBool("ControlRotation", false, new JSONStorableBool.SetBoolCallback(v =>
        {
            possessRotationLink.valNoCallback = null;
            Reapply();
        }));

        rotationLockNoRollJSON = new JSONStorableBool("NoRoll", false, new JSONStorableBool.SetBoolCallback(v => Reapply()));

        Func<List<string>> getPossessRotationChoices = () =>
        {
            var controllers = containingAtom.freeControllers.Where(x => x.possessable && x.canGrabRotation).Select(x => x.name).ToList();
            controllers.Insert(0, _targetNone);
            return controllers;
        };
        possessRotationLink = new JSONStorableStringChooser("PossessRotationLink", getPossessRotationChoices(), _targetNone, "Possess rotation of", (string val) =>
        {
            rotationLockJSON.valNoCallback = false;
            Reapply();
        });

        rotationSmoothingJSON = new JSONStorableFloat("Rotation Smoothing", 0f, 0f, 1f, true);

        rotationOffsetXjson = new JSONStorableFloat("Rotation X", 0f, SyncRotationOffset, -180, 180, true, true);

        rotationOffsetYjson = new JSONStorableFloat("Rotation Y", 0f, SyncRotationOffset, -180, 180, true, true);

        rotationOffsetZjson = new JSONStorableFloat("Rotation Z", 0f, SyncRotationOffset, -180, 180, true, true);

        positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0f, 0f, 1f, true);

        positionOffsetXjson = new JSONStorableFloat("Position X", 0f,  SyncPositionOffset, -2f, 2f, false, true);

        positionOffsetYjson = new JSONStorableFloat("Position Y", 0.08f, SyncPositionOffset, -2f, 2f, false, true);

        positionOffsetZjson = new JSONStorableFloat("Position Z", 0f, SyncPositionOffset, -2f, 2f, false, true);

        eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0.1f, new JSONStorableFloat.SetFloatCallback(v => Reapply()), 0f, 0.2f, false);

        SyncPositionOffset(0);
        SyncRotationOffset(0);
    }

    private void SyncRotationOffset(float _)
    {
        _rotationOffset = Quaternion.Euler(rotationOffsetXjson.val, rotationOffsetYjson.val, rotationOffsetZjson.val);
    }

    private void SyncPositionOffset(float _)
    {
        _positionOffset = new Vector3(positionOffsetXjson.val, positionOffsetYjson.val, positionOffsetZjson.val);
    }

    private void Reapply()
    {
        if (!enabled) return;
        enabled = false;
        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        // ReSharper disable once Unity.InefficientPropertyAccess
        enabled = true;
        UpdateNavigationRig(true);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        try
        {
            _link = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == linkJSON.val);
            if (lookAtJSON.val)
                _lookAt = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
            if (possessRotationLink.val != _targetNone)
                _possessedLink = containingAtom.linkableRigidbodies.FirstOrDefault(rb => rb.name == possessRotationLink.val);

            if (!CanActivate() || !IsValid())
            {
                enabled = false;
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
            enabled = false;
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

    public override void OnDisable()
    {
        base.OnDisable();

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
                enabled = false;
                return;
            }

            UpdateNavigationRig(false);
        }
        catch (Exception e)
        {
            SuperController.LogError($"Embody: Failed to apply Passenger.\n{e}");
            enabled = false;
        }
    }

    private void UpdateNavigationRig(bool force)
    {
        // Idea!!! Set player height to zero :)
        // Context
        var positionSmoothing = positionSmoothingJSON.val;
        var rotationSmoothing = rotationSmoothingJSON.val;
        var navigationRig = SuperController.singleton.navigationRig;
        // TODO: All of this should be greatly simplified. Find the eye center and offset from there.
        /*
            var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
            _lEye = eyes.First(eye => eye.name == "lEye").transform;
            _rEye = eyes.First(eye => eye.name == "rEye").transform;
            var eyesDistance = Vector3.Distance(_lEye.position, _rightEye.position)
            // TODO: Now, parent something to lEye and offset by lEye.right * (eyesDistance / 2f)
         */
        var eyesToHeadOffset = new Vector3(0, 0, eyesToHeadDistanceJSON.val);

        // TODO: Use the context motion control instead with the custom possess point!
        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _link.transform;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation;

        if (!ReferenceEquals(_lookAt, null) && lookAtWeightJSON.val > 0)
        {
            var lookAtRotation = Quaternion.LookRotation(_lookAt.transform.position - linkTransform.position, linkTransform.up);
            rotation = Quaternion.Slerp(rotation, lookAtRotation, lookAtWeightJSON.val);
        }

        rotation *= _rotationOffset;

        if (rotationLockNoRollJSON.val)
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

        if (force || rotationLockJSON.val)
        {
            if (!ReferenceEquals(_possessedLink, null))
                _possessedLink.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation * _rotationOffset;
            else
                navigationRigTransform.rotation = navigationRigRotation;
        }

        if (force || positionLockJSON.val)
        {
            navigationRigTransform.position = navigationRigPosition;
        }

        SuperController.singleton.SyncMonitorRigPosition();

        // TODO: Re-position the main menu and the hud anchors (ovr) if that's possible
        // TODO: If we can move the navigation rig during fixed update (see ovr) we could stabilize before vam does raycasting & positioning
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        // TODO: Store Vector3 instead
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        // TODO: Store Vector3 instead
    }
}
