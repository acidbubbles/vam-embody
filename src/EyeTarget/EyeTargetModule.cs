using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IEyeTargetModule : IEmbodyModule
{
    JSONStorableBool trackMirrorsJSON { get; }
    JSONStorableBool trackObjectsJSON { get; }
    void Rescan();
}

public class EyeTargetModule : EmbodyModuleBase, IEyeTargetModule
{
    public const string Label = "Eye Target";
    private static readonly HashSet<string> _mirrorAtomTypes = new HashSet<string>(new[]
    {
        "Glass",
        "Glass-Stained",
        "ReflectiveSlate",
        "ReflectiveWoodPanel",
    });

    public override string storeId => "EyeTarget";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;

    public JSONStorableBool trackMirrorsJSON { get; private set; }
    public JSONStorableBool trackObjectsJSON { get; private set; }

    private EyesControl _eyeBehavior;
    private Transform _head;
    private Transform _lEye;
    private Transform _rEye;
    private FreeControllerV3 _eyeTarget;
    private readonly List<BoxCollider> _mirrors = new List<BoxCollider>();
    private readonly List<Transform> _objects = new List<Transform>();
    private Vector3 _eyeTargetRestorePosition;
    private EyesControl.LookMode _eyeBehaviorRestoreLookMode;

    public override void Awake()
    {
        base.Awake();

        _eyeBehavior = (EyesControl) containingAtom.GetStorableByID("Eyes");
        _head = containingAtom.rigidbodies.First(fc => fc.name == "head").transform;
        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        _lEye = eyes.First(eye => eye.name == "lEye").transform;
        _rEye = eyes.First(eye => eye.name == "rEye").transform;
        _eyeTarget = containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");
        trackMirrorsJSON = new JSONStorableBool("TrackMirrors", true, (bool _) => { if(enabled) Rescan(); });
        trackObjectsJSON = new JSONStorableBool("TrackObjects", true, (bool _) => { if(enabled) Rescan(); });
    }

    public override bool BeforeEnable()
    {
        Rescan();

        SuperController.LogMessage($"{_objects.Count}");

        return _mirrors.Count > 0 || _objects.Count > 0;
    }

    public void Rescan()
    {
        UpdateMirrors();
        UpdateObjects();
    }

    public override void OnEnable()
    {
        base.OnEnable();

        _eyeTargetRestorePosition = _eyeTarget.control.position;
        _eyeBehaviorRestoreLookMode = _eyeBehavior.currentLookMode;

        _eyeBehavior.currentLookMode = EyesControl.LookMode.Target;

        SuperController.singleton.onAtomUIDsChangedHandlers += ONAtomUIDsChanged;
    }

    private void UpdateMirrors()
    {
        _mirrors.Clear();

        if (!trackMirrorsJSON.val) return;

        _mirrors.AddRange(SuperController.singleton.GetAtoms()
            .Where(a => _mirrorAtomTypes.Contains(a.type))
            .Where(a => a.on)
            .Select(a => a.GetComponentInChildren<BoxCollider>())
            .Where(c => c != null));
    }

    private void UpdateObjects()
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
                    if (atom.GetStorableByID("CameraControl")?.GetBoolParamValue("cameraOn") != true) continue;
                    _objects.Add(atom.mainController.control);
                    break;
                }
                case "Person":
                {
                    foreach (var controller in atom.freeControllers.Where(fc => fc.name.EndsWith("Control")))
                    {
                        _objects.Add(controller.control);
                    }
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

         _mirrors.Clear();
         _objects.Clear();
    }

    public void Update()
    {
        if (_objects.Count > 0)
        {
            var lastDistance = float.PositiveInfinity;
            var lastPosition = Vector3.zero;
            var planes = GeometryUtility.CalculateFrustumPlanes(SuperController.singleton.centerCameraTarget.targetCamera);
            var cameraPosition = SuperController.singleton.centerCameraTarget.transform.position;

            foreach (var o in _objects)
            {
                var position = o.position;
                var bounds = new Bounds(position, new Vector3(0.25f, 0.25f, 0.25f));
                if (!GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
                var distance = Vector3.Distance(bounds.center, cameraPosition);
                if (distance > lastDistance) continue;
                lastDistance = distance;
                lastPosition = position;
            }

            if (!float.IsPositiveInfinity(lastDistance))
            {
                _eyeTarget.control.position = lastPosition;
                return;
            }
        }

        var eyesCenter = (_lEye.position + _rEye.position) / 2f;
        BoxCollider lookAtMirror;
        if (_mirrors.Count == 1)
        {
            lookAtMirror = _mirrors[0];
        }
        else
        {
            var headPosition = _head.position;
            var ray = new Ray(eyesCenter, _head.forward);
            var lookAtMirrorDistance = float.PositiveInfinity;
            lookAtMirror = null;
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
                if (hit.distance > lookAtMirrorDistance) continue;
                lookAtMirrorDistance = hit.distance;
                lookAtMirror = potentialMirror;
            }

            if (ReferenceEquals(lookAtMirror, null))
            {
                if (ReferenceEquals(closestMirror, null)) return;
                lookAtMirror = closestMirror;
            }
        }

        var mirrorTransform = lookAtMirror.transform;
        var mirrorPosition = mirrorTransform.position;
        var mirrorNormal = mirrorTransform.up;
        var plane = new Plane(mirrorNormal, mirrorPosition);
        var planePoint = plane.ClosestPointOnPlane(eyesCenter);
        var reflectPosition = planePoint - (eyesCenter - planePoint);
        _eyeTarget.control.position = reflectPosition;
    }

    private void ONAtomUIDsChanged(List<string> uids)
    {
        Rescan();
    }
}
