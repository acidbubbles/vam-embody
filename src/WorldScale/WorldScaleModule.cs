using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IWorldScaleModule : IEmbodyModule
{
    JSONStorableBool useProfileJSON { get; }
    JSONStorableStringChooser worldScaleMethodJSON { get; }
    JSONStorableFloat playerHeightJSON { get; }
    JSONStorableFloat fixedWorldScaleJSON { get; }
    void ApplyWorldScale();
    void ClearPersonalData();
}

public class WorldScaleModule : EmbodyModuleBase, IWorldScaleModule
{
    public const string Label = "World Scale";
    public const string NoneMethod = "None";
    public const string PlayerHeightMethod = "Player Height";
    public const string EyeDistanceMethod = "Eyes Distance";

    public override string storeId => "WorldScale";
    public override string label => Label;
    public JSONStorableBool useProfileJSON { get; } = new JSONStorableBool("ImportDefaultsOnLoad", true);
    public JSONStorableStringChooser worldScaleMethodJSON { get; } = new JSONStorableStringChooser("WorldScaleMethod", new List<string>{NoneMethod, EyeDistanceMethod, PlayerHeightMethod}, EyeDistanceMethod, "Method");
    public JSONStorableFloat playerHeightJSON { get; } = new JSONStorableFloat("PlayerHeight", 1.7f, 0f, 2f, false);
    public JSONStorableFloat fixedWorldScaleJSON { get; } = new JSONStorableFloat("FixedWorldScale", 0, 0, 10, false);

    private float _originalWorldScale;
    private bool _originalShowNavigationHologrid;

    public override void InitStorables()
    {
        base.InitStorables();
        selectedJSON.defaultVal = context.containingAtom.type == "Person";
        worldScaleMethodJSON.defaultVal = context.containingAtom.type == "Person" ? EyeDistanceMethod : NoneMethod;
        fixedWorldScaleJSON.setCallbackFunction = val => fixedWorldScaleJSON.valNoCallback = Mathf.Clamp(fixedWorldScaleJSON.val, 0, SuperController.singleton.worldScaleSlider.maxValue);
    }

    public override bool Validate()
    {
        if (worldScaleMethodJSON.val == NoneMethod && fixedWorldScaleJSON.val == 0) return false;
        return base.Validate();
    }

    public override void PreActivate()
    {
        _originalShowNavigationHologrid = SuperController.singleton.showNavigationHologrid;
        _originalWorldScale = SuperController.singleton.worldScale;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        SuperController.singleton.showNavigationHologrid = false;

        ApplyWorldScale();
    }

    public void ApplyWorldScale()
    {
        float worldScale;
        if (fixedWorldScaleJSON.val > 0)
        {
            SuperController.singleton.worldScale = fixedWorldScaleJSON.val;
            return;
        }

        switch (worldScaleMethodJSON.val)
        {
            case NoneMethod:
                return;
            case EyeDistanceMethod:
                worldScale = UseEyeDistanceMethod();
                break;
            case PlayerHeightMethod:
                worldScale = UsePersonHeightMethod();
                break;
            default:
                throw new NotImplementedException($"Unknown method: '{worldScaleMethodJSON.val}'");
        }

        if (worldScale > 0f && Mathf.Abs(worldScale - SuperController.singleton.worldScale) > 0.005f)
            SuperController.singleton.worldScale = worldScale;
    }

    public override void PostDeactivate()
    {
        if (_originalWorldScale == 0f) return;

        SuperController.singleton.worldScale = _originalWorldScale;
        // TODO: Figure out a way to avoid that. Probably wait one second before re-enabling it...
        if (_originalShowNavigationHologrid)
            SuperController.singleton.showNavigationHologrid = true;
    }

    private float UseEyeDistanceMethod()
    {
        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.FirstOrDefault(eye => eye.name == "lEye");
        var rEye = eyes.FirstOrDefault(eye => eye.name == "rEye");
        if (lEye == null || rEye == null)
            return 0;
        var atomEyeDistance = Vector3.Distance(lEye.transform.position, rEye.transform.position);

        var rigEyesDistance = GetRigEyesDistance();
        if (rigEyesDistance <= float.Epsilon)
            return 0;

        var scale = atomEyeDistance / rigEyesDistance;
        var worldScale = SuperController.singleton.worldScale * scale;

        if (Math.Abs(SuperController.singleton.worldScale - worldScale) < 0.0001f)
            return 0;

        return worldScale;
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

    private float UsePersonHeightMethod()
    {
        if (playerHeightJSON.val == 0) return 0f;

        var measurements = new PersonMeasurements(context);
        var measure = measurements.MeasureHeight();

        var ratio = measure / playerHeightJSON.val;
        return ratio;
    }

    public void ClearPersonalData()
    {
        playerHeightJSON.valNoCallback = playerHeightJSON.defaultVal;
        worldScaleMethodJSON.valNoCallback = EyeDistanceMethod;
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        if (toScene)
        {
            useProfileJSON.StoreJSON(jc);
            fixedWorldScaleJSON.StoreJSON(jc);
        }

        if (toScene && !useProfileJSON.val || toProfile)
        {
            worldScaleMethodJSON.StoreJSON(jc);
            playerHeightJSON.StoreJSON(jc);
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromScene)
        {
            useProfileJSON.RestoreFromJSON(jc);
            fixedWorldScaleJSON.RestoreFromJSON(jc);
        }

        if (fromScene && !useProfileJSON.val || fromProfile)
        {
            worldScaleMethodJSON.RestoreFromJSON(jc);
            playerHeightJSON.RestoreFromJSON(jc);
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        useProfileJSON.SetValToDefault();
        fixedWorldScaleJSON.SetValToDefault();
        worldScaleMethodJSON.SetValToDefault();
        playerHeightJSON.SetValToDefault();
    }
}
