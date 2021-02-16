using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    JSONStorableBool positionLockJSON { get; }
    JSONStorableFloat positionSmoothingJSON { get; }
    JSONStorableBool allowPersonHeadRotationJSON { get; }
    JSONStorableFloat eyesToHeadDistanceOffsetJSON { get; }
    Vector3 positionOffset { get; set; }
    Vector3 rotationOffset { get; set; }
}

public class PassengerModule : EmbodyModuleBase, IPassengerModule
{
    public const string Label = "Passenger";

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
    public JSONStorableFloat eyesToHeadDistanceOffsetJSON { get; set; }

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

    protected override bool shouldBeSelectedByDefault => context.containingAtom.type != "Person";

    private Rigidbody _headRigidbody;
    private Transform _headTransform;
    private FreeControllerV3 _headControl;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private UserPreferences _preferences;
    private RigidbodyInterpolation _previousInterpolation;
    private FreeControllerV3 _eyeTargetControl;
    private Transform _cameraCenterTarget;
    private Transform _cameraCenter;
    private float _headToEyesDistance;
    private NavigationRigSnapshot _navigationRigSnapshot;
    private FreeControllerV3Snapshot _headControlSnapshot;

    public override void Awake()
    {
        base.Awake();

        _preferences = SuperController.singleton.GetAtomByUid("CoreControl").gameObject.GetComponent<UserPreferences>();
        _headControl = containingAtom.freeControllers.FirstOrDefault(rb => rb.name == "headControl") ?? containingAtom.mainController;
        _headRigidbody = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "head") ?? containingAtom.rigidbodies.First();
        // ReSharper disable once Unity.NoNullPropagation
        _headTransform = (containingAtom.type == "Person" ? containingAtom.GetComponentInChildren<LookAtWithLimits>()?.transform.parent : null) ?? _headRigidbody.transform;
        _eyeTargetControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");

        _cameraCenter = new GameObject("Passenger_CameraCenter").transform;
        _cameraCenter.SetParent(_headTransform, false);
        _cameraCenterTarget = new GameObject("Passenger_CameraCenterTarget").transform;
        _cameraCenterTarget.SetParent(_cameraCenter, false);

        lookAtJSON = new JSONStorableBool("LookAtEyeTarget", false, (bool val) => Reapply());

        lookAtWeightJSON = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);

        positionLockJSON = new JSONStorableBool("ControlPosition", true, (bool val) => Reapply());

        rotationLockJSON = new JSONStorableBool("ControlRotation", true, val =>
        {
            if(val) allowPersonHeadRotationJSON.valNoCallback = false;
            Reapply();
        });

        rotationLockNoRollJSON = new JSONStorableBool("NoRoll", false, (bool val) => Reapply());

        allowPersonHeadRotationJSON = new JSONStorableBool("AllowPersonHeadRotationJSON", false, val =>
        {
            if (_eyeTargetControl == null)
            {
                allowPersonHeadRotationJSON.valNoCallback = false;
                return;
            }
            if (val) rotationLockJSON.valNoCallback = false;
            Reapply();
        });

        rotationSmoothingJSON = new JSONStorableFloat("RotationSmoothing", 0f, 0f, 1f, true);

        positionSmoothingJSON = new JSONStorableFloat("PositionSmoothing", 0f, 0f, 1f, true);

        eyesToHeadDistanceOffsetJSON = new JSONStorableFloat("EyesToHeadDistanceOffset", 0f, (float val) => Reapply(), -0.1f, 0.2f, false);
    }

    private void Reapply()
    {
        if (!enabled) return;
        enabled = false;
        // ReSharper disable once Unity.InefficientPropertyAccess
        enabled = true;
    }

    public override bool BeforeEnable()
    {
        if (_preferences.useHeadCollider)
        {
            SuperController.LogError("Embody: Do not enable the head collider with Passenger, they do not work together!");
            return false;
        }
        if (context.containingAtom.type == "Person")
        {
            var eyes = _headTransform.GetComponentsInChildren<LookAtWithLimits>();
            var lEye = eyes.First(eye => eye.name == "lEye").transform;
            var rEye = eyes.First(eye => eye.name == "rEye").transform;
            var eyesCenter = (lEye.localPosition + rEye.localPosition) / 2f;
            var upDelta = eyesCenter.y;
            _cameraCenter.localPosition = new Vector3(0f, upDelta, 0f);
            _headToEyesDistance = eyesToHeadDistanceOffsetJSON.val + Vector3.Distance(eyesCenter, _cameraCenter.localPosition);
        }
        else
        {
            _cameraCenter.localPosition = Vector3.zero;
        }
        return true;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        _headControlSnapshot = FreeControllerV3Snapshot.Snap(_headControl);
        _headControl.canGrabPosition = false;
        _headControl.canGrabRotation = false;

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

        _previousInterpolation = _headRigidbody.interpolation;
        _headRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

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

        if (_headControlSnapshot != null)
        {
            if (allowPersonHeadRotationJSON.val)
            {
                _headControlSnapshot.Restore(true);
            }
            else
            {
                _headControl.canGrabPosition = _headControlSnapshot.canGrabPosition;
                _headControl.canGrabRotation = _headControlSnapshot.canGrabRotation;
            }

            _headControlSnapshot = null;
        }

        _currentPositionVelocity = Vector3.zero;
        _currentRotationVelocity = Quaternion.identity;
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

    [MethodImpl(256)]
    private void UpdateNavigationRig(bool force)
    {
        // Context
        var positionSmoothing = positionSmoothingJSON.val;
        var rotationSmoothing = rotationSmoothingJSON.val;
        var navigationRig = SuperController.singleton.navigationRig;
        var centerTargetTransform = CameraTarget.centerTarget.transform;
        var navigationRigTransform = navigationRig.transform;
        var linkTransform = _cameraCenterTarget;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation;

        if (lookAtJSON.val && lookAtWeightJSON.val > 0)
        {
            var lookAtRotation = Quaternion.LookRotation(_eyeTargetControl.transform.position - linkTransform.position, linkTransform.up);
            rotation = Quaternion.Slerp(rotation, lookAtRotation, lookAtWeightJSON.val);
        }

        if (rotationLockNoRollJSON.val)
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);

        // Move navigation rig

        var cameraDelta = centerTargetTransform.position
                          - navigationRigTransform.position
                          - centerTargetTransform.rotation * new Vector3(0, 0, _headToEyesDistance * SuperController.singleton.worldScale);
        var navigationRigPosition = position - cameraDelta;

        var navigationRigRotation = rotation;

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
        if (allowPersonHeadRotationJSON.val)
        {
            _headControl.transform.rotation = CameraTarget.centerTarget.targetCamera.transform.rotation;
        }

        if (force || positionLockJSON.val)
        {
            navigationRigTransform.position = navigationRigPosition;
        }
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        lookAtJSON.StoreJSON(jc);
        lookAtWeightJSON.StoreJSON(jc);
        rotationLockJSON.StoreJSON(jc);
        rotationLockNoRollJSON.StoreJSON(jc);
        rotationSmoothingJSON.StoreJSON(jc);
        positionLockJSON.StoreJSON(jc);
        positionSmoothingJSON.StoreJSON(jc);
        allowPersonHeadRotationJSON.StoreJSON(jc);
        eyesToHeadDistanceOffsetJSON.StoreJSON(jc);
        jc["PositionOffset"] = positionOffset.ToJSON();
        jc["RotationOffset"] = rotationOffset.ToJSON();
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        lookAtJSON.RestoreFromJSON(jc);
        lookAtWeightJSON.RestoreFromJSON(jc);
        rotationLockJSON.RestoreFromJSON(jc);
        rotationLockNoRollJSON.RestoreFromJSON(jc);
        rotationSmoothingJSON.RestoreFromJSON(jc);
        positionLockJSON.RestoreFromJSON(jc);
        positionSmoothingJSON.RestoreFromJSON(jc);
        allowPersonHeadRotationJSON.RestoreFromJSON(jc);
        eyesToHeadDistanceOffsetJSON.RestoreFromJSON(jc);
        positionOffset = jc["PositionOffset"].ToVector3(Vector3.zero);
        rotationOffset = jc["RotationOffset"].ToVector3(Vector3.zero);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();
        positionOffset = Vector3.zero;
        rotationOffset = Vector3.zero;
        lookAtJSON.SetValToDefault();
        positionLockJSON.SetValToDefault();
        positionSmoothingJSON.SetValToDefault();
        rotationLockJSON.SetValToDefault();
        rotationSmoothingJSON.SetValToDefault();
        lookAtWeightJSON.SetValToDefault();
        allowPersonHeadRotationJSON.SetValToDefault();
        rotationLockNoRollJSON.SetValToDefault();
        eyesToHeadDistanceOffsetJSON.SetValToDefault();
    }
}
