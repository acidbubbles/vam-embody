using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IEyeTargetModule : IEmbodyModule
{
    JSONStorableBool trackMirrorsJSON { get; }
    JSONStorableBool trackWindowCameraJSON { get; }
    JSONStorableBool trackSelfHandsJSON { get; }
    JSONStorableBool trackSelfGenitalsJSON { get; }
    JSONStorableBool trackPersonsJSON { get; }
    JSONStorableBool trackObjectsJSON { get; }
    JSONStorableFloat frustrumJSON { get; }
    JSONStorableFloat gazeMinDurationJSON { get; }
    JSONStorableFloat gazeMaxDurationJSON { get; }
    JSONStorableFloat shakeMinDurationJSON { get; }
    JSONStorableFloat shakeMaxDurationJSON { get; }
    JSONStorableFloat shakeRangeJSON { get; }
    void Rescan();
}

public class EyeTargetModule : EmbodyModuleBase, IEyeTargetModule
{
    public const string Label = "Eye Target";
    private const float _mirrorScanSpan = 0.5f;
    private const float _objectScanSpan = 0.1f;
    private const float _naturalLookDistance = 0.4f;

    private static readonly HashSet<string> _mirrorAtomTypes = new HashSet<string>(new[]
    {
        "Glass",
        "Glass-Stained",
        "ReflectiveSlate",
        "ReflectiveWoodPanel",
    });

    private static readonly HashSet<string> _bonesLookAt = new HashSet<string>
    {
        "lHand",
        "rHand",
        "lEye",
        "rEye",
        "tongue03",
        "abdomen",
        "chest",
        "pelvis",
        "Gen3",
        "Testes",
    };

    public override string storeId => "EyeTarget";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;

    public JSONStorableBool trackMirrorsJSON { get; } = new JSONStorableBool("TrackMirrors", true);
    public JSONStorableBool trackWindowCameraJSON { get; } = new JSONStorableBool("TrackWindowCamera", true);
    public JSONStorableBool trackSelfHandsJSON { get; } = new JSONStorableBool("TrackSelfHands", true);
    public JSONStorableBool trackSelfGenitalsJSON { get; } = new JSONStorableBool("TrackSelfGenitals", true);
    public JSONStorableBool trackPersonsJSON { get; } = new JSONStorableBool("TrackPersons", true);
    public JSONStorableBool trackObjectsJSON { get; } = new JSONStorableBool("TrackObjects", true);
    public JSONStorableFloat frustrumJSON { get; } = new JSONStorableFloat("FrustrumFOV", 6f, 0f, 45f, true);
    public JSONStorableFloat gazeMinDurationJSON { get; } = new JSONStorableFloat("GazeMinDuration", 0.5f, 0f, 10f, false);
    public JSONStorableFloat gazeMaxDurationJSON { get; } = new JSONStorableFloat("GazeMaxDuration", 2f, 0f, 10f, false);
    public JSONStorableFloat shakeMinDurationJSON { get; } = new JSONStorableFloat("ShakeMinDuration", 0.2f, 0f, 1f, false);
    public JSONStorableFloat shakeMaxDurationJSON { get; } = new JSONStorableFloat("ShakeMaxDuration", 0.5f, 0f, 1f, false);
    public JSONStorableFloat shakeRangeJSON { get; } = new JSONStorableFloat("ShakeRangeDuration", 0.015f, 0f, 0.1f, true);

    private EyesControl _eyeBehavior;
    private Transform _head;
    private Transform _lEye;
    private Transform _rEye;
    private FreeControllerV3 _eyeTarget;
    private readonly List<BoxCollider> _mirrors = new List<BoxCollider>();
    private readonly List<Transform> _objects = new List<Transform>();
    private Vector3 _eyeTargetRestorePosition;
    private EyesControl.LookMode _eyeBehaviorRestoreLookMode;
    private readonly Plane[] _frustrumPlanes = new Plane[6];
    private readonly List<EyeTargetReference> _lockTargetCandidates = new List<EyeTargetReference>();
    private BoxCollider _lookAtMirror;
    private float _lookAtMirrorDistance;
    private float _nextMirrorScanTime;
    private float _nextObjectsScanTime;
    private float _nextLockTargetTime;
    private float _nextShakeTime;
    private Vector3 _shakeValue;
    private Transform _lockTarget;

    public override void Awake()
    {
        base.Awake();

        _eyeBehavior = (EyesControl) containingAtom.GetStorableByID("Eyes");
        _head = context.bones.First(eye => eye.name == "head").transform;
        _lEye = context.bones.First(eye => eye.name == "lEye").transform;
        _rEye = context.bones.First(eye => eye.name == "rEye").transform;
        _eyeTarget = containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");
        trackMirrorsJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        trackWindowCameraJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        trackSelfHandsJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        trackSelfGenitalsJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        trackPersonsJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        trackObjectsJSON.setCallbackFunction = _ => { if(enabled) Rescan(); };
        gazeMinDurationJSON.setCallbackFunction = val => gazeMaxDurationJSON.valNoCallback = Mathf.Max(val, gazeMaxDurationJSON.val);
        gazeMaxDurationJSON.setCallbackFunction = val => gazeMinDurationJSON.valNoCallback = Mathf.Min(val, gazeMinDurationJSON.val);
        shakeMinDurationJSON.setCallbackFunction = val => shakeMaxDurationJSON.valNoCallback = Mathf.Max(val, shakeMaxDurationJSON.val);
        shakeMaxDurationJSON.setCallbackFunction = val => shakeMinDurationJSON.valNoCallback = Mathf.Min(val, shakeMinDurationJSON.val);
    }

    public override bool BeforeEnable()
    {
        Rescan();

        return _mirrors.Count > 0 || _objects.Count > 0;
    }

    public void Rescan()
    {
        ClearState();
        SyncMirrors();
        SyncObjects();

    }

    public override void OnEnable()
    {
        base.OnEnable();

        _eyeTargetRestorePosition = _eyeTarget.control.position;
        _eyeBehaviorRestoreLookMode = _eyeBehavior.currentLookMode;

        _eyeBehavior.currentLookMode = EyesControl.LookMode.Target;

        SuperController.singleton.onAtomUIDsChangedHandlers += ONAtomUIDsChanged;
    }

    private void SyncMirrors()
    {
        _mirrors.Clear();

        if (!trackMirrorsJSON.val) return;

        _mirrors.AddRange(SuperController.singleton.GetAtoms()
            .Where(a => _mirrorAtomTypes.Contains(a.type))
            .Where(a => a.on)
            .Select(a => a.GetComponentInChildren<BoxCollider>())
            .Where(c => c != null));
    }

    private void SyncObjects()
    {
        _objects.Clear();

        if (!trackObjectsJSON.val) return;

        foreach (var atom in SuperController.singleton.GetAtoms())
        {
            if (!atom.on) continue;

            switch (atom.type)
            {
                case "WindowCamera":
                {
                    if (!trackWindowCameraJSON.val) continue;
                    if (atom.GetStorableByID("CameraControl")?.GetBoolParamValue("cameraOn") != true) continue;
                    _objects.Add(atom.mainController.control);
                    break;
                }
                case "Person":
                {
                    if (atom == containingAtom)
                    {
                        foreach (var bone in context.bones)
                        {
                            if (trackSelfHandsJSON.val && (bone.name == "lHand" || bone.name == "rHand"))
                                _objects.Add(bone.transform);
                            else if (trackSelfGenitalsJSON.val && (bone.name == "Gen1" || bone.name == "Gen3"))
                                _objects.Add(bone.transform);
                        }
                        continue;
                    }

                    if (!trackPersonsJSON.val) continue;
                    foreach (var bone in atom.transform.Find("rescale2").GetComponentsInChildren<DAZBone>())
                    {
                        if (!_bonesLookAt.Contains(bone.name)) continue;
                        _objects.Add(bone.transform);
                    }
                    break;
                }
                case "Cube":
                case "Sphere":
                case "Dildo":
                case "Paddle":
                case "ToyAH":
                case "ToyBP":
                case "CustomUnityAsset":
                case "Torch":
                {
                    if (!trackObjectsJSON.val) continue;
                    _objects.Add(atom.mainController.control);
                    break;
                }
            }
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        SuperController.singleton.onAtomUIDsChangedHandlers -= ONAtomUIDsChanged;

         _eyeTarget.control.position = _eyeTargetRestorePosition;
         if(_eyeBehavior.currentLookMode != EyesControl.LookMode.Target)
             _eyeBehavior.currentLookMode = _eyeBehaviorRestoreLookMode;

         ClearState();
    }

    private void ClearState()
    {
        _lookAtMirror = null;
        _mirrors.Clear();
        _objects.Clear();
        _lockTargetCandidates.Clear();
        _nextMirrorScanTime = 0f;
        _nextObjectsScanTime = 0f;
        _nextLockTargetTime = 0f;
        _nextShakeTime = 0f;
        _shakeValue = Vector3.zero;
    }

    public void Update()
    {
        var eyesCenter = (_lEye.position + _rEye.position) / 2f;

        ScanMirrors(eyesCenter);
        ScanObjects(eyesCenter);
        SelectLockTarget();

        if (!ReferenceEquals(_lockTarget, null))
        {
            SelectShake();
            _eyeTarget.control.position = _lockTarget.transform.position + _shakeValue;
            return;
        }

        if (!ReferenceEquals(_lookAtMirror, null))
        {
            var mirrorTransform = _lookAtMirror.transform;
            var mirrorPosition = mirrorTransform.position;
            var mirrorNormal = mirrorTransform.up;
            var plane = new Plane(mirrorNormal, mirrorPosition);
            var planePoint = plane.ClosestPointOnPlane(eyesCenter);
            var reflectPosition = planePoint - (eyesCenter - planePoint);
            _eyeTarget.control.position = reflectPosition;
            return;
        }

        _eyeTarget.control.position = eyesCenter + _head.forward * _naturalLookDistance;
    }

    private void SelectShake()
    {
        if (_nextShakeTime > Time.time) return;
        _nextShakeTime = Time.time + Random.Range(shakeMinDurationJSON.val, shakeMaxDurationJSON.val);

        _shakeValue = Random.insideUnitSphere * shakeRangeJSON.val;
    }

    private void SelectLockTarget()
    {
        if (_nextLockTargetTime > Time.time) return;
        _nextLockTargetTime = Time.time + Random.Range(gazeMinDurationJSON.val, gazeMaxDurationJSON.val);

        _lockTarget = _lockTargetCandidates.Count > 0
            ? _lockTargetCandidates[Random.Range(0, _lockTargetCandidates.Count)].transform
            : null;
        _shakeValue = Vector3.zero;
        _nextShakeTime = Time.time + Random.Range(shakeMinDurationJSON.val, shakeMaxDurationJSON.val);
    }

    private void ScanObjects(Vector3 eyesCenter)
    {
        if (_nextObjectsScanTime > Time.time) return;
        _nextObjectsScanTime = Time.time + _objectScanSpan;

        if (_objects.Count == 0) return;

        var originalCount = _lockTargetCandidates.Count;
        _lockTargetCandidates.Clear();

        //var planes = GeometryUtility.CalculateFrustumPlanes(SuperController.singleton.centerCameraTarget.targetCamera);
        CalculateFrustum(eyesCenter, _head.forward, frustrumJSON.val * Mathf.Deg2Rad, 1.3f, 0.35f, 100f, _frustrumPlanes);

        Transform closest = null;
        var closestDistance = float.PositiveInfinity;
        foreach (var o in _objects)
        {
            var position = o.position;
            var bounds = new Bounds(position, new Vector3(0.25f, 0.25f, 0.25f));
            if (!GeometryUtility.TestPlanesAABB(_frustrumPlanes, bounds)) continue;
            var distance = Vector3.Distance(bounds.center, eyesCenter);
            if (distance > _lookAtMirrorDistance) continue;
            _lockTargetCandidates.Add(new EyeTargetReference
            {
                transform = o,
                distance = distance
            });
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = o;
            }
        }

        if (_lockTargetCandidates.Count != originalCount)
        {
            if (_lockTargetCandidates.Count > 0)
            {
                _lockTarget = closest;
                _nextLockTargetTime = Time.time + Random.Range(gazeMinDurationJSON.val, gazeMaxDurationJSON.val);
            }

            _nextLockTargetTime = 0;
        }
    }

    private void ScanMirrors(Vector3 eyesCenter)
    {
        if (_nextMirrorScanTime > Time.time) return;
        _nextMirrorScanTime = Time.time + _mirrorScanSpan;

        _lookAtMirror = null;
        _lookAtMirrorDistance = float.PositiveInfinity;

        if (_mirrors.Count <= 0)
            return;

        var headPosition = _head.position;

        if (_mirrors.Count == 1)
        {
            _lookAtMirror = _mirrors[0];
            _lookAtMirrorDistance = Vector3.Distance(headPosition, _lookAtMirror.transform.position);
            return;
        }

        var ray = new Ray(eyesCenter, _head.forward);
        var closestMirrorDistance = float.PositiveInfinity;
        BoxCollider closestMirror = null;
        for (var i = 0; i < _mirrors.Count; i++)
        {
            var potentialMirror = _mirrors[i];
            var potentialMirrorDistance = Vector3.Distance(headPosition, potentialMirror.transform.position);
            if (potentialMirrorDistance < closestMirrorDistance)
            {
                closestMirrorDistance = potentialMirrorDistance;
                closestMirror = potentialMirror;
            }

            RaycastHit hit;
            if (!potentialMirror.Raycast(ray, out hit, 20f))
                continue;
            if (hit.distance > _lookAtMirrorDistance) continue;
            _lookAtMirrorDistance = hit.distance;
            _lookAtMirror = potentialMirror;
        }

        if (ReferenceEquals(_lookAtMirror, null))
        {
            if (ReferenceEquals(closestMirror, null)) return;
            _lookAtMirror = closestMirror;
        }
    }

    // Source: http://answers.unity.com/answers/1024526/view.html
    private static void CalculateFrustum(Vector3 origin, Vector3 direction, float fovRadians, float viewRatio, float near, float far, Plane[] frustrumPlanes)
    {
        var nearCenter = origin + direction * near;
        var farCenter = origin + direction * far;
        var camRight = Vector3.Cross(direction, Vector3.up) * -1;
        var camUp = Vector3.Cross(direction, camRight);
        var nearHeight = 2 * Mathf.Tan(fovRadians / 2) * near;
        var farHeight = 2 * Mathf.Tan(fovRadians / 2) * far;
        var nearWidth = nearHeight * viewRatio;
        var farWidth = farHeight * viewRatio;
        var farTopLeft = farCenter + camUp * (farHeight * 0.5f) - camRight * (farWidth * 0.5f);
        //not needed; 6 points are sufficient to calculate the frustum
        //Vector3 farTopRight = farCenter + camUp*(farHeight*0.5f) + camRight*(farWidth*0.5f);
        var farBottomLeft = farCenter - camUp * (farHeight * 0.5f) - camRight * (farWidth * 0.5f);
        var farBottomRight = farCenter - camUp * (farHeight * 0.5f) + camRight * (farWidth * 0.5f);
        var nearTopLeft = nearCenter + camUp * (nearHeight * 0.5f) - camRight * (nearWidth * 0.5f);
        var nearTopRight = nearCenter + camUp * (nearHeight * 0.5f) + camRight * (nearWidth * 0.5f);
        //not needed; 6 points are sufficient to calculate the frustum
        //Vector3 nearBottomLeft  = nearCenter - camUp*(nearHeight*0.5f) - camRight*(nearWidth*0.5f);
        var nearBottomRight = nearCenter - camUp * (nearHeight * 0.5f) + camRight * (nearWidth * 0.5f);
        frustrumPlanes[0] = new Plane(nearTopLeft, farTopLeft, farBottomLeft);
        frustrumPlanes[1] = new Plane(nearTopRight, nearBottomRight, farBottomRight);
        frustrumPlanes[2] = new Plane(farBottomLeft, farBottomRight, nearBottomRight);
        frustrumPlanes[3] = new Plane(farTopLeft, nearTopLeft, nearTopRight);
        frustrumPlanes[4] = new Plane(nearBottomRight, nearTopRight, nearTopLeft);
        frustrumPlanes[5] = new Plane(farBottomRight, farBottomLeft, farTopLeft);
    }

    private void ONAtomUIDsChanged(List<string> uids)
    {
        Rescan();
    }

    public override void StoreJSON(JSONClass jc, bool includeProfile)
    {
        base.StoreJSON(jc, includeProfile);

        trackMirrorsJSON.StoreJSON(jc);
        trackWindowCameraJSON.StoreJSON(jc);
        trackSelfHandsJSON.StoreJSON(jc);
        trackSelfGenitalsJSON.StoreJSON(jc);
        trackPersonsJSON.StoreJSON(jc);
        trackObjectsJSON.StoreJSON(jc);
        frustrumJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromDefaults)
    {
        base.RestoreFromJSON(jc, fromDefaults);

        trackMirrorsJSON.RestoreFromJSON(jc);
        trackWindowCameraJSON.RestoreFromJSON(jc);
        trackSelfHandsJSON.RestoreFromJSON(jc);
        trackSelfGenitalsJSON.RestoreFromJSON(jc);
        trackPersonsJSON.RestoreFromJSON(jc);
        trackObjectsJSON.RestoreFromJSON(jc);
        frustrumJSON.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        trackMirrorsJSON.SetValToDefault();
        trackWindowCameraJSON.SetValToDefault();
        trackSelfHandsJSON.SetValToDefault();
        trackSelfGenitalsJSON.SetValToDefault();
        trackPersonsJSON.SetValToDefault();
        trackObjectsJSON.SetValToDefault();
        frustrumJSON.SetValToDefault();
    }

    public struct EyeTargetReference
    {
        public float distance;
        public Transform transform;
    }
}
