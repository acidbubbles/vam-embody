﻿using System;
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
        if (rigEyesDistance <= float.Epsilon)
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
        if (SuperController.singleton.OVRRig != null)
        {
            var ovrRig = SuperController.singleton.OVRRig.GetComponent<OVRCameraRig>();
            if (ovrRig != null && ovrRig.leftEyeAnchor != null && ovrRig.rightEyeAnchor != null)
                return Vector3.Distance(ovrRig.leftEyeAnchor.transform.position, ovrRig.rightEyeAnchor.transform.position);
        }

        return 0;
    }

    private void UsePersonHeightMethod()
    {
        if (playerHeightJSON.val == 0) return;

        var measurements = new PersonMeasurements(context);
        var measure = measurements.MeasureHeight();

        var ratio = measure / playerHeightJSON.val;
        SuperController.singleton.worldScale = ratio;
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
