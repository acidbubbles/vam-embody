using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using MeshVR;
using SimpleJSON;
using UnityEngine;

public interface IPassengerModule : IEmbodyModule
{
    JSONStorableBool useProfileJSON { get; }
    JSONStorableBool exitOnMenuOpen { get; }
    JSONStorableBool lookAtJSON { get; }
    JSONStorableFloat lookAtWeightJSON { get; }
    JSONStorableBool rotationLockJSON { get; }
    JSONStorableBool rotationLockNoRollJSON { get; }
    JSONStorableBool rotationLockNoTiltJSON { get; }
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

    public JSONStorableBool useProfileJSON { get; } = new JSONStorableBool("ImportDefaultsOnLoad", true);
    public JSONStorableBool exitOnMenuOpen { get; } = new JSONStorableBool("ExitOnMenuOpen", true);
    public JSONStorableBool lookAtJSON { get; } = new JSONStorableBool("LookAtEyeTarget", false);
    public JSONStorableFloat lookAtWeightJSON { get; } = new JSONStorableFloat("LookAtWeight", 1f, 0f, 1f);
    public JSONStorableBool rotationLockJSON { get; } = new JSONStorableBool("ControlRotation", true);
    public JSONStorableBool rotationLockNoRollJSON { get; } = new JSONStorableBool("NoRoll", false);
    public JSONStorableBool rotationLockNoTiltJSON { get; } = new JSONStorableBool("NoTilt", false);
    public JSONStorableFloat rotationSmoothingJSON { get; } = new JSONStorableFloat("RotationSmoothing", 0f, 0f, 1f, true);
    public JSONStorableBool positionLockJSON { get; } = new JSONStorableBool("ControlPosition", true);
    public JSONStorableFloat positionSmoothingJSON { get; } = new JSONStorableFloat("PositionSmoothing", 0f, 0f, 1f, true);
    public JSONStorableBool allowPersonHeadRotationJSON { get; } = new JSONStorableBool("AllowPersonHeadRotationJSON", false);
    public JSONStorableFloat eyesToHeadDistanceOffsetJSON { get; }  = new JSONStorableFloat("EyesToHeadDistanceOffset", 0f, -0.1f, 0.2f, false);

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

    private Rigidbody _headControlRB;
    private Transform _headBoneTransform;
    private FreeControllerV3 _headControl;
    private Quaternion _currentRotationVelocity;
    private Vector3 _currentPositionVelocity;
    private RigidbodyInterpolation _previousInterpolation;
    private FreeControllerV3 _eyeTargetControl;
    private Transform _cameraCenterTarget;
    private Transform _cameraCenter;
    private float _headToEyesDistance;
    private FreeControllerV3Snapshot _headControlSnapshot;
    private Vector3 _rigRotationOffsetSource = Vector3.zero;
    private Quaternion _rigRotationOffset = Quaternion.identity;

    public override void InitStorables()
    {
        base.InitStorables();

        selectedJSON.defaultVal = context.containingAtom.type != "Person";

        eyesToHeadDistanceOffsetJSON.setCallbackFunction = _ => SyncCameraParent();
        rotationLockJSON.setCallbackFunction = val =>
        {
            if (val) allowPersonHeadRotationJSON.valNoCallback = false;
            SyncCameraParent();
        };
        rotationLockNoRollJSON.setCallbackFunction = val =>
        {
            if (!val) rotationLockNoTiltJSON.val = false;
        };
        rotationLockNoTiltJSON.setCallbackFunction = val =>
        {
            if (val) rotationLockNoRollJSON.val = true;
        };
        allowPersonHeadRotationJSON.setCallbackFunction = val =>
        {
            if (val) rotationLockJSON.valNoCallback = false;
            SyncCameraParent();
        };

        // Initialized early because this contains the actual storable offsets
        _cameraCenter = new GameObject("Passenger_CameraCenter").transform;
        _cameraCenterTarget = new GameObject("Passenger_CameraCenterTarget").transform;
        _cameraCenterTarget.SetParent(_cameraCenter, false);
    }

    public override void InitReferences()
    {
        base.InitReferences();

        _headControl = containingAtom.freeControllers.FirstOrDefault(rb => rb.name == "headControl") ?? containingAtom.mainController;
        _headControlRB = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "head") ?? containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "object") ?? containingAtom.rigidbodies.First();
        // ReSharper disable once Unity.NoNullPropagation
        _headBoneTransform = (containingAtom.type == "Person" ? context.bones.FirstOrDefault(b => b.name == "head")?.transform : null) ?? _headControlRB.transform;
        _eyeTargetControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "eyeTargetControl");

        _cameraCenter.SetParent(_headBoneTransform, false);

        SyncCameraParent();
    }

    public override void PreActivate()
    {
        SyncCameraParent();

        if (exitOnMenuOpen.val)
        {
            SuperController.singleton.HideMainHUD();
        }

        _rigRotationOffsetSource = Vector3.zero;
        _rigRotationOffset = Quaternion.identity;
    }

    private void SyncCameraParent()
    {
        if (ReferenceEquals(_cameraCenter, null)) return;

        if (context.containingAtom.type == "Person")
            SyncCameraParentForPerson();
        else
            _cameraCenter.localPosition = Vector3.zero;
    }

    private void SyncCameraParentForPerson()
    {
        if (context.trackers.selectedJSON.val || allowPersonHeadRotationJSON.val)
        {
            // This avoids hands influencing head position, and in turn head position influencing hands causing havoc
            _cameraCenter.SetParent(_headControlRB.transform, false);
        }
        else
        {
            // This avoids stretched head control moving outside of the head center
            _cameraCenter.SetParent(_headBoneTransform, false);
        }

        var lEye = context.bones.First(b => b.name == "lEye").transform;
        var rEye = context.bones.First(b => b.name == "rEye").transform;
        var eyesCenter = (lEye.localPosition + rEye.localPosition) / 2f;
        var upDelta = eyesCenter.y;
        _cameraCenter.localPosition = new Vector3(0f, upDelta, 0f);
        _headToEyesDistance = eyesToHeadDistanceOffsetJSON.val + Vector3.Distance(eyesCenter, _cameraCenter.localPosition);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        UserPreferences.singleton.headCollider.gameObject.SetActive(false);

        if (SuperController.singleton.MonitorCenterCamera != null)
        {
            var monitorCenterCameraTransform = SuperController.singleton.MonitorCenterCamera.transform;
            monitorCenterCameraTransform.localEulerAngles = Vector3.zero;
        }

        _headControlSnapshot = FreeControllerV3Snapshot.Snap(_headControl);
        _headControl.canGrabPosition = false;
        _headControl.canGrabRotation = false;

        _previousInterpolation = _headControlRB.interpolation;
        if (!allowPersonHeadRotationJSON.val)
            _headControlRB.interpolation = RigidbodyInterpolation.Interpolate;

        if (rotationLockJSON.val)
            GlobalSceneOptions.singleton.disableNavigation = true;

        StartCoroutine(OnEnableCo());
    }

    private IEnumerator OnEnableCo()
    {
        // This will be offset by VaM, but should be pretty close for small world scale differences
        UpdateNavigationRig(true);
        yield return new WaitForEndOfFrame();
        // This will bring back a perfect match when Control Position is off
        UpdateNavigationRig(true);
        // In some cases, another adjustment will still be required
        yield return 0f;
        UpdateNavigationRig(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        GlobalSceneOptions.singleton.disableNavigation = false;

        if (_headControlRB != null)
            _headControlRB.interpolation = _previousInterpolation;

        if (_headControlSnapshot != null)
        {
            if (allowPersonHeadRotationJSON.val)
            {
                _headControlSnapshot.Restore(false);
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

        UserPreferences.singleton.headCollider.gameObject.SetActive(UserPreferences.singleton.useHeadCollider);
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
            if (exitOnMenuOpen.val && SuperController.singleton.mainHUD.gameObject.activeSelf)
            {
                context.embody.Deactivate();
                return;
            }

            const float yawSpeed = 40f;
            const float pitchSpeed = 32f;
            _rigRotationOffsetSource += new Vector3(
                JoystickControl.GetAxis(SuperController.singleton.navigationUpAxis) * pitchSpeed,
                JoystickControl.GetAxis(SuperController.singleton.navigationTurnAxis) * yawSpeed,
                0f) * Time.unscaledDeltaTime;
            _rigRotationOffset = Quaternion.identity * Quaternion.Euler(_rigRotationOffsetSource);

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
        var linkTransform = _cameraCenterTarget;

        // Desired camera position

        var position = linkTransform.position;
        var rotation = linkTransform.rotation * _rigRotationOffset;

        if (lookAtJSON.val && lookAtWeightJSON.val > 0 && !ReferenceEquals(_eyeTargetControl, null))
        {
            var lookAtRotation = Quaternion.LookRotation(_eyeTargetControl.transform.position - linkTransform.position, linkTransform.up);
            lookAtRotation.eulerAngles += _cameraCenterTarget.localEulerAngles;
            rotation = Quaternion.Slerp(rotation, lookAtRotation, lookAtWeightJSON.val);
        }

        if (rotationLockNoRollJSON.val)
        {
            rotation.SetLookRotation(rotation * Vector3.forward, Vector3.up);
            if (rotationLockNoTiltJSON.val)
                rotation = Quaternion.Euler(new Vector3(0, rotation.eulerAngles.y, 0));
        }

        // Navigation rig rotation

        var navigationRigRotation = rotation;

        if (!force)
        {
            if (rotationSmoothing > 0)
                navigationRigRotation = navigationRig.rotation.SmoothDamp(navigationRigRotation, ref _currentRotationVelocity, rotationSmoothing);
        }

        if (force || rotationLockJSON.val)
        {
            navigationRig.rotation = navigationRigRotation;
        }
        else if (allowPersonHeadRotationJSON.val)
        {
            var cameraRotation = CameraTarget.centerTarget.targetCamera.transform.rotation;
            _headControl.control.rotation = cameraRotation;
            _headControlRB.transform.rotation = cameraRotation;
        }

        // Navigation rig position

        var cameraDelta = centerTargetTransform.position
                          - navigationRig.position
                          - centerTargetTransform.rotation * new Vector3(0, 0, _headToEyesDistance * SuperController.singleton.worldScale);
        var navigationRigPosition = position - cameraDelta;


        if (!force)
        {
            if (positionSmoothing > 0)
                navigationRigPosition = Vector3.SmoothDamp(navigationRig.position, navigationRigPosition, ref _currentPositionVelocity, positionSmoothing, Mathf.Infinity, Time.smoothDeltaTime);
        }

        if (force || positionLockJSON.val)
        {
            navigationRig.position = navigationRigPosition;
        }
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        if (toScene)
        {
            useProfileJSON.StoreJSON(jc);
        }

        if (toScene && !useProfileJSON.val || toProfile)
        {
            exitOnMenuOpen.StoreJSON(jc);
        }

        lookAtJSON.StoreJSON(jc);
        lookAtWeightJSON.StoreJSON(jc);
        rotationLockJSON.StoreJSON(jc);
        rotationLockNoRollJSON.StoreJSON(jc);
        rotationLockNoTiltJSON.StoreJSON(jc);
        rotationSmoothingJSON.StoreJSON(jc);
        positionLockJSON.StoreJSON(jc);
        positionSmoothingJSON.StoreJSON(jc);
        allowPersonHeadRotationJSON.StoreJSON(jc);
        eyesToHeadDistanceOffsetJSON.StoreJSON(jc);
        jc["PositionOffset"] = positionOffset.ToJSON();
        jc["RotationOffset"] = rotationOffset.ToJSON();
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromScene)
        {
            useProfileJSON.RestoreFromJSON(jc);
        }

        if (fromScene && !useProfileJSON.val || fromProfile)
        {
            exitOnMenuOpen.RestoreFromJSON(jc);
        }

        lookAtJSON.RestoreFromJSON(jc);
        lookAtWeightJSON.RestoreFromJSON(jc);
        rotationLockJSON.RestoreFromJSON(jc);
        rotationLockNoRollJSON.RestoreFromJSON(jc);
        rotationLockNoTiltJSON.RestoreFromJSON(jc);
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
        useProfileJSON.SetValToDefault();
        lookAtJSON.SetValToDefault();
        positionLockJSON.SetValToDefault();
        positionSmoothingJSON.SetValToDefault();
        rotationLockJSON.SetValToDefault();
        rotationSmoothingJSON.SetValToDefault();
        lookAtWeightJSON.SetValToDefault();
        allowPersonHeadRotationJSON.SetValToDefault();
        rotationLockNoRollJSON.SetValToDefault();
        rotationLockNoTiltJSON.SetValToDefault();
        eyesToHeadDistanceOffsetJSON.SetValToDefault();
        positionOffset = Vector3.zero;
        rotationOffset = Vector3.zero;
    }
}
