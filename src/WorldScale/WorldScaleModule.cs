using System;
using System.Linq;
using UnityEngine;

// TODO: Probably to deprecate... or based on player height v.s. model height if I can figure out sitting model height...
public interface IWorldScale : IEmbodyModule
{
}

public class WorldScaleModule : EmbodyModuleBase, IWorldScale
{
    public JSONStorableBool activeJSON { get; set; }

    private float _originalWorldScale;
    private Possessor _possessor;
    private FreeControllerV3 _headControl;

    public override void Init()
    {
        base.Init();

        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");

        // TODO: Option for using eyes, vr height, or manual
    }

    public override void OnEnable()
    {
        base.OnEnable();

        // TODO: Disable the hologrid here? SuperController.singleton.showNavigationHologrid
        ApplyAutoWorldScale();
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_originalWorldScale == 0f) return;

        SuperController.singleton.worldScale = _originalWorldScale;
        _originalWorldScale = 0f;
    }

    private void ApplyAutoWorldScale()
    {
        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.FirstOrDefault(eye => eye.name == "lEye");
        var rEye = eyes.FirstOrDefault(eye => eye.name == "rEye");
        if (lEye == null || rEye == null)
            return;
        var atomEyeDistance = Vector3.Distance(lEye.transform.position, rEye.transform.position);

        var rigEyesDistance = GetRigEyesDistance();
        if (rigEyesDistance == 0)
            return;

        var scale = atomEyeDistance / rigEyesDistance;
        var worldScale = SuperController.singleton.worldScale * scale;

        if (Math.Abs(SuperController.singleton.worldScale - worldScale) < 0.0001f)
            return;

        _originalWorldScale = SuperController.singleton.worldScale;
        SuperController.singleton.worldScale = worldScale;

        var yAdjust = _possessor.autoSnapPoint.position.y - _headControl.possessPoint.position.y;

        if (yAdjust != 0)
            SuperController.singleton.playerHeightAdjust -= yAdjust;
    }

    private static float GetRigEyesDistance()
    {
        // TODO: Do it for Steam too
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig == null)
            return 0;
        return Vector3.Distance(rig.leftEyeAnchor.transform.position, rig.rightEyeAnchor.transform.position);
    }
}
