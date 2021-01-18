using System;
using System.Linq;
using MeshVR;
using SimpleJSON;
using UnityEngine;

public interface IPassengerModule : IEmbodyModule
{
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
    JSONStorableBool allowPersonHeadRotationJSON { get; }
}

public class PassengerModule : EmbodyModuleBase, IPassengerModule
{
    public const string Label = "Passenger";

    private const string _targetNone = "none";

    public override string storeId => "Passenger";
    public override string label => Label;

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
    public JSONStorableBool allowPersonHeadRotationJSON { get; set; }

    private Rigidbody _headRigidbody;
    private FreeControllerV3 _headController;
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
    private FreeControllerV3 _eyeTargetControl;
    private Transform _cameraCenterTarget;

    public override void Awake()
    {
        base.Awake();

        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

        lookAtJSON = new JSONStorableBool("LookAtEyeTarget", false, (bool val) => Reapply());

        lookAtWeightJSON = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);

        positionLockJSON = new JSONStorableBool("ControlPosition", true, (bool val) => Reapply());

        rotationLockJSON = new JSONStorableBool("ControlRotation", false, (bool val) =>
        {
            if(val) allowPersonHeadRotationJSON.valNoCallback = false;
            Reapply();
        });

        rotationLockNoRollJSON = new JSONStorableBool("NoRoll", false, (bool val) => Reapply());

        allowPersonHeadRotationJSON = new JSONStorableBool("AllowPersonHeadRotationJSON", false, (bool val) =>
        {
            if (val) rotationLockJSON.valNoCallback = false;
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

        _headRigidbody = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "head") ?? containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "object");
        if (_headRigidbody == null) throw new NullReferenceException("Embody: Could not find a link");

        if (lookAtJSON.val)
            _eyeTargetControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");
        if (allowPersonHeadRotationJSON.val)
            _headController = containingAtom.freeControllers.FirstOrDefault(rb => rb.name == "headControl");

        if (_preferences.useHeadCollider)
        {
            SuperController.LogError("Embody: Do not enable the head collider with Passenger, they do not work together!");
            enabled = false;
            return;
        }

        if (context.containingAtom.type == "Person")
        {
            var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
            var lEye = eyes.First(eye => eye.name == "lEye").transform;
            var rEye = eyes.First(eye => eye.name == "rEye").transform;
            _cameraCenterTarget = new GameObject("Passenger_CameraCenterTarget").transform;
            _cameraCenterTarget.SetParent(lEye, false);
            _cameraCenterTarget.position = (lEye.position + rEye.position) / 2f;
            _cameraCenterTarget.rotation = _headRigidbody.rotation;
        }
        else
        {
            _cameraCenterTarget = new GameObject("Passenger_CameraCenterTarget").transform;
            _cameraCenterTarget.SetParent(_headRigidbody.transform, false);
        }

        var superController = SuperController.singleton;
        var navigationRig = superController.navigationRig;

        _previousRotation = navigationRig.rotation;
        _previousPosition = navigationRig.position;

        _previousInterpolation = _headRigidbody.interpolation;
        _headRigidbody.interpolation = RigidbodyInterpolation.Extrapolate;

        var offsetStartRotation = !superController.MonitorRig.gameObject.activeSelf;
        if (offsetStartRotation)
            _startRotationOffset = Quaternion.Euler(0, navigationRig.eulerAngles.y - _possessor.transform.eulerAngles.y, 0f);

        GlobalSceneOptions.singleton.disableNavigation = true;

        UpdateNavigationRig(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        GlobalSceneOptions.singleton.disableNavigation = false;

        SuperController.singleton.navigationRig.rotation = _previousRotation;
        SuperController.singleton.navigationRig.position = _previousPosition;

        if(_cameraCenterTarget != null)
            Destroy(_cameraCenterTarget.gameObject);

        if (_headRigidbody != null)
            _headRigidbody.interpolation = _previousInterpolation;

        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        _startRotationOffset = Quaternion.identity;

        _headRigidbody = null;
        _headController = null;
        _eyeTargetControl = null;
    }

    public void Update()
    {
        try
        {
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
        // Context
        var positionSmoothing = positionSmoothingJSON.val;
        var rotationSmoothing = rotationSmoothingJSON.val;
        var navigationRig = SuperController.singleton.navigationRig;
        // TODO: Further simplify
        // TODO: Use the context motion control instead with the custom possess point!
        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _cameraCenterTarget;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation;

        if (!ReferenceEquals(_eyeTargetControl, null) && lookAtWeightJSON.val > 0)
        {
            var lookAtRotation = Quaternion.LookRotation(_eyeTargetControl.transform.position - linkTransform.position, linkTransform.up);
            rotation = Quaternion.Slerp(rotation, lookAtRotation, lookAtWeightJSON.val);
        }

        rotation *= _rotationOffset;

        if (rotationLockNoRollJSON.val)
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);

        position += rotation * _positionOffset;

        // Move navigation rig

        var cameraDelta = centerTargetTransform.position - navigationRigTransform.position;
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
            if (!ReferenceEquals(_headController, null))
                _headController.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation * _rotationOffset;
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
