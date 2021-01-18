using System;
using System.Linq;
using MeshVR;
using SimpleJSON;
using UnityEngine;

public interface IPassengerModule : IEmbodyModule
{
    // TODO: This is incompatible with mirror lookat. They should exclude each other.
    JSONStorableBool lookAtJSON { get; }
    JSONStorableFloat lookAtWeightJSON { get; }
    JSONStorableBool rotationLockJSON { get; }
    JSONStorableBool rotationLockNoRollJSON { get; }
    JSONStorableFloat rotationSmoothingJSON { get; }
    JSONStorableBool positionLockJSON { get; }
    JSONStorableFloat positionSmoothingJSON { get; }
    JSONStorableBool allowPersonHeadRotationJSON { get; }
    JSONStorableFloat eyesToHeadDistanceJSON { get; }
    Vector3 positionOffset { get; set; }
    Vector3 rotationOffset { get; set; }
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
    public JSONStorableBool positionLockJSON { get; set; }
    public JSONStorableFloat positionSmoothingJSON { get; set; }
    public JSONStorableBool allowPersonHeadRotationJSON { get; set; }
    public JSONStorableFloat eyesToHeadDistanceJSON { get; set; }

    public Vector3 positionOffset
    {
        get { return _cameraCenterTarget.localPosition; }
        set { _cameraCenterTarget.localPosition = value; }
    }

    public Vector3 rotationOffset
    {
        get { return _cameraCenterTarget.localEulerAngles; }
        set { _cameraCenterTarget.localEulerAngles = value; }
    }

    private Rigidbody _headRigidbody;
    private FreeControllerV3 _headController;
    private Possessor _possessor;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private Quaternion _startRotationOffset;
    private RigidbodyInterpolation _previousInterpolation;
    private FreeControllerV3 _eyeTargetControl;
    private Transform _cameraCenterTarget;
    private NavigationRigSnapshot _navigationRigSnapshot;
    private Transform _cameraCenter;
    private float _headToEyesDistance;

    public override void Awake()
    {
        base.Awake();

        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
        _headRigidbody = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "head") ?? containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "object");
        if (_headRigidbody == null) throw new NullReferenceException("Embody: Could not find a link");

        _cameraCenter = new GameObject("Passenger_CameraCenter").transform;
        _cameraCenter.SetParent(_headRigidbody.transform, false);
        _cameraCenterTarget = new GameObject("Passenger_CameraCenterTarget").transform;
        _cameraCenterTarget.SetParent(_cameraCenter, false);
        /*
        TODO: Optionally attach to a spring joint
        var cameraCenterTargetRigidbody = _cameraCenter.gameObject.AddComponent<Rigidbody>();
        cameraCenterTargetRigidbody.detectCollisions = false;

        var cameraCenterTarget2 = new GameObject("Passenger_CameraCenterTarget2");
        var cameraCenterTarget2Rigidbody = _cameraCenter.gameObject.AddComponent<Rigidbody>();
        cameraCenterTarget2Rigidbody.detectCollisions = false;
        */

        lookAtJSON = new JSONStorableBool("LookAtEyeTarget", false, (bool val) => Reapply());

        lookAtWeightJSON = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);

        positionLockJSON = new JSONStorableBool("ControlPosition", true, (bool val) => Reapply());

        rotationLockJSON = new JSONStorableBool("ControlRotation", true, (bool val) =>
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

        positionSmoothingJSON = new JSONStorableFloat("Position Smoothing", 0f, 0f, 1f, true);

        eyesToHeadDistanceJSON = new JSONStorableFloat("Eyes-To-Head Distance", 0f, (float val) => Reapply(), -0.1f, 0.2f, false);
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

        _cameraCenter.rotation = _headRigidbody.rotation;
        _cameraCenter.position = _headRigidbody.position;
        if (context.containingAtom.type == "Person")
        {
            var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
            var lEye = eyes.First(eye => eye.name == "lEye").transform;
            var rEye = eyes.First(eye => eye.name == "rEye").transform;
            var eyesCenter = (lEye.position + rEye.position) / 2f;
            var d = Vector3.Dot(eyesCenter, _headRigidbody.transform.up);
            _cameraCenter.position = new Vector3(0f, d, 0f);
            // TODO: There should be another JSON for offset and a calculated value
            _headToEyesDistance = eyesToHeadDistanceJSON.val + Vector3.Distance(eyesCenter, _cameraCenter.position);
        }

        // TODO: Disable grabbing

        var superController = SuperController.singleton;
        var navigationRig = superController.navigationRig;

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

        _previousInterpolation = _headRigidbody.interpolation;
        // TODO: Try with this and none, see what's best
        _headRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        var offsetStartRotation = !superController.MonitorRig.gameObject.activeSelf;
        if (offsetStartRotation)
            _startRotationOffset = Quaternion.Euler(0, navigationRig.eulerAngles.y - _possessor.transform.eulerAngles.y, 0f);

        if (!allowPersonHeadRotationJSON.val)
            GlobalSceneOptions.singleton.disableNavigation = true;

        UpdateNavigationRig(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        GlobalSceneOptions.singleton.disableNavigation = false;

        _navigationRigSnapshot?.Restore();

        if (_headRigidbody != null)
            _headRigidbody.interpolation = _previousInterpolation;

        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
        _startRotationOffset = Quaternion.identity;

        _headController = null;
        _eyeTargetControl = null;
    }

    public void OnDestroy()
    {
        if (_cameraCenterTarget != null) Destroy(_cameraCenterTarget.gameObject);
        if (_cameraCenter != null) Destroy(_cameraCenter.gameObject);
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

        if (rotationLockNoRollJSON.val)
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);

        // Move navigation rig

        var cameraDelta = centerTargetTransform.position
                          - navigationRigTransform.position
                          - centerTargetTransform.rotation * new Vector3(0, 0, _headToEyesDistance);
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
            navigationRigTransform.rotation = navigationRigRotation;
        }

        if (!ReferenceEquals(_headController, null))
        {
            _headController.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation;
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
