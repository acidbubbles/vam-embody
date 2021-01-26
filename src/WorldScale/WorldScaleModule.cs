using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IWorldScaleModule : IEmbodyModule
{
    JSONStorableStringChooser worldScaleMethodJSON { get; }
    JSONStorableFloat playerHeightJSON { get; }
    void ClearPersonalData();
}

public class WorldScaleModule : EmbodyModuleBase, IWorldScaleModule
{
    public const string Label = "World Scale";
    public const string PlayerHeightMethod = "Player Height";
    public const string EyeDistanceMethod = "Eyes Distance";
    private const float _feetToGroundDistance = 0.059f;
    private const float _skeletonSumToStandHeightRatio = 0.863f;

    public override string storeId => "WorldScale";
    public override string label => Label;
    protected override bool shouldBeSelectedByDefault => true;
    public JSONStorableStringChooser worldScaleMethodJSON { get; } = new JSONStorableStringChooser("WorldScaleMethod", new List<string>{EyeDistanceMethod, PlayerHeightMethod}, EyeDistanceMethod, "Method");
    public JSONStorableFloat playerHeightJSON { get; } = new JSONStorableFloat("PlayerHeight", 1.7f, 0f, 2f, false);

    private float _originalWorldScale;
    private bool _originalShowNavigationHologrid;
    private Possessor _possessor;
    private FreeControllerV3 _headControl;

    public override void Awake()
    {
        base.Awake();

        _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");
    }

    public override void OnEnable()
    {
        base.OnEnable();

        _originalShowNavigationHologrid = SuperController.singleton.showNavigationHologrid;
        _originalWorldScale = SuperController.singleton.worldScale;

        SuperController.singleton.showNavigationHologrid = false;

        switch (worldScaleMethodJSON.val)
        {
            case EyeDistanceMethod:
                UseEyeDistanceMethod();
                break;
            case PlayerHeightMethod:
                UsePersonHeightMethod();
                break;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_originalWorldScale == 0f) return;

        SuperController.singleton.worldScale = _originalWorldScale;
        // TODO: Figure out a way to avoid that. Probably wait one second before re-enabling it...
        if (_originalShowNavigationHologrid)
            SuperController.singleton.showNavigationHologrid = true;
        _originalWorldScale = 0f;
    }

    private void UseEyeDistanceMethod()
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
        var ovrRig = SuperController.singleton.OVRRig.GetComponent<OVRCameraRig>();
        if (ovrRig != null)
            return Vector3.Distance(ovrRig.leftEyeAnchor.transform.position, ovrRig.rightEyeAnchor.transform.position);

        return 0;
    }

    private void UsePersonHeightMethod()
    {
        if (playerHeightJSON.val == 0) return;

        var measure = Distance(
            Get("head"),
            Get("neck"),
            Get("chest"),
            Get("abdomen2"),
            Get("abdomen"),
            Get("hip"),
            Get("pelvis"),
            Get("rThigh"),
            Get("rShin"),
            Get("rFoot")
        );

        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.First(eye => eye.name == "lEye").transform;
        var eyesToHeadDistance = Mathf.Abs(transform.InverseTransformPoint(lEye.position).y - transform.InverseTransformPoint(Get("head").position).y);

        measure += eyesToHeadDistance + _feetToGroundDistance;
        measure *= _skeletonSumToStandHeightRatio;

        var ratio = measure / playerHeightJSON.val;
        SuperController.singleton.worldScale = ratio;
    }

    private static float Distance(params Rigidbody[] rigidbodies)
    {
        var total = 0f;
        for (var i = 0; i < rigidbodies.Length - 1; i++)
        {
            total += Vector3.Distance(rigidbodies[i].position, rigidbodies[i + 1].position);
        }
        return total;
    }

    private Rigidbody Get(string rbName)
    {
        return context.containingAtom.rigidbodies.First(rb => rb.name == rbName);
    }

    public void ClearPersonalData()
    {
        playerHeightJSON.valNoCallback = playerHeightJSON.defaultVal;
        worldScaleMethodJSON.valNoCallback = EyeDistanceMethod;
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        worldScaleMethodJSON.StoreJSON(jc);
        playerHeightJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        worldScaleMethodJSON.RestoreFromJSON(jc);
        playerHeightJSON.RestoreFromJSON(jc);
    }
}
