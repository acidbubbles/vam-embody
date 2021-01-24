using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IEyeTargetModule : IEmbodyModule
{
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

    private EyesControl _eyeBehavior;
    private Transform _head;
    private Transform _lEye;
    private Transform _rEye;
    private FreeControllerV3 _eyeTarget;
    private List<BoxCollider> _mirrors;
    private Vector3 _eyeTargetRestorePosition;
    private EyesControl.LookMode _eyeBehaviorRestoreLookMode;
    private Transform _windowCamera;

    public override void Awake()
    {
        base.Awake();

        _eyeBehavior = (EyesControl) containingAtom.GetStorableByID("Eyes");
        _head = containingAtom.rigidbodies.First(fc => fc.name == "head").transform;
        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        _lEye = eyes.First(eye => eye.name == "lEye").transform;
        _rEye = eyes.First(eye => eye.name == "rEye").transform;
        _eyeTarget = containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");
    }

    public override bool BeforeEnable()
    {
        _mirrors = GetMirrors();
        _windowCamera = GetWindowCamera();

        return _mirrors.Count > 0 || !ReferenceEquals(_windowCamera, null);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        _eyeTargetRestorePosition = _eyeTarget.control.position;
        _eyeBehaviorRestoreLookMode = _eyeBehavior.currentLookMode;

        _eyeBehavior.currentLookMode = EyesControl.LookMode.Target;
    }

    private static List<BoxCollider> GetMirrors()
    {
        // TODO: Add WindowCamera (if no mirrors, and direction is around the window camera (e.g. in a 20% fov), look at the window camera directly
        return SuperController.singleton.GetAtoms()
            .Where(a => _mirrorAtomTypes.Contains(a.type))
            .Where(a => a.on)
            .Select(a => a.GetComponentInChildren<BoxCollider>())
            .Where(c => c != null)
            .ToList();
    }

    private static Transform GetWindowCamera()
    {
        var atom = SuperController.singleton.GetAtoms()
            .FirstOrDefault(a => a.type == "WindowCamera");
        if (atom == null) return null;
        if (atom.GetStorableByID("CameraControl")?.GetBoolParamValue("cameraOn") != true) return null;
        return atom.mainController.control;
    }

    public override void OnDisable()
    {
        base.OnDisable();

         _eyeTarget.control.position = _eyeTargetRestorePosition;
         if(_eyeBehavior.currentLookMode != EyesControl.LookMode.Target)
             _eyeBehavior.currentLookMode = _eyeBehaviorRestoreLookMode;
    }

    public void Update()
    {
        if (_mirrors.Count == 0 && !ReferenceEquals(_windowCamera, null))
        {
            _eyeTarget.control.position = _windowCamera.position;
            return;
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
}
