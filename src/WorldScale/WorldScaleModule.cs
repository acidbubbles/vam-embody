using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IWorldScaleModule : IEmbodyModule
{
    JSONStorableStringChooser worldScaleMethodJSON { get; }
    JSONStorableFloat playerHeightJSON { get; }
    JSONStorableFloat fixedWorldScaleJSON { get; }
    void ApplyWorldScale();
    void UsePlayerHeight(float height);
    void ClearPersonalData();
}

public class WorldScaleModule : EmbodyModuleBase, IWorldScaleModule
{
    public const string Label = "World Scale";
    public const string NoneMethod = "None";
    public const string PlayerHeightMethod = "Player Height";
    public const string EyeDistanceMethod = "Eyes Distance";

    public override string storeId => "WorldScale";
    protected override bool selectedFromProfile => true;
    public override string label => Label;
    public JSONStorableStringChooser worldScaleMethodJSON { get; } = new JSONStorableStringChooser("WorldScaleMethod", new List<string>{NoneMethod, EyeDistanceMethod, PlayerHeightMethod}, EyeDistanceMethod, "Method");
    public JSONStorableFloat playerHeightJSON { get; } = new JSONStorableFloat("PlayerHeight", 1.7f, 0f, 2f, false);
    public JSONStorableFloat fixedWorldScaleJSON { get; } = new JSONStorableFloat("FixedWorldScale", 0, 0, 10, false);

    // The method: the scene's when it sets one (e.g. None), otherwise the player's (marked **).
    private readonly SceneFirstSettings _sceneFirst = new SceneFirstSettings();
    private float _originalWorldScale;
    private bool _originalShowNavigationHologrid;
    private Transform _navigationHologrid;
    private Transform _fakeHologrid;

    public override void InitStorables()
    {
        base.InitStorables();
        selectedJSON.defaultVal = context.containingAtom.type == "Person";
        selectedJSON.valNoCallback = selectedJSON.defaultVal;
        worldScaleMethodJSON.defaultVal = context.containingAtom.type == "Person" ? EyeDistanceMethod : NoneMethod;
        fixedWorldScaleJSON.setCallbackFunction = val => fixedWorldScaleJSON.valNoCallback = Mathf.Clamp(fixedWorldScaleJSON.val, 0, SuperController.singleton.worldScaleSlider.maxValue);
        _sceneFirst.Add(worldScaleMethodJSON);
    }

    // Recording the player's height is calibration, not a choice for the scene, so the method it switches to is not
    // marked as set by the scene.
    public void UsePlayerHeight(float height)
    {
        playerHeightJSON.val = height;
        _sceneFirst.ChangeWithoutMarking(() => worldScaleMethodJSON.val = PlayerHeightMethod);
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
        if (_fakeHologrid == null)
            _fakeHologrid = new GameObject().transform;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        var sc = SuperController.singleton;
        sc.showNavigationHologrid = false;
        _navigationHologrid = sc.navigationHologrid;
        if (_navigationHologrid != null)
        {
            sc.navigationHologrid = _fakeHologrid;
            var meshRenderer = _navigationHologrid.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.material.SetFloat("_Alpha", 0);
            _navigationHologrid.gameObject.SetActive(false);
        }

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

        var sc = SuperController.singleton;
        sc.worldScale = _originalWorldScale;
        if (_navigationHologrid != null)
        {
            sc.navigationHologrid = _navigationHologrid;
            _navigationHologrid = null;
        }
        if (_originalShowNavigationHologrid)
            sc.showNavigationHologrid = true;
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
        _sceneFirst.ChangeWithoutMarking(() => worldScaleMethodJSON.valNoCallback = EyeDistanceMethod);
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);
        _sceneFirst.StoreJSON(jc, toProfile, toScene);

        if (toScene)
            fixedWorldScaleJSON.StoreJSON(jc);

        // The player's height is personal, so it is saved in profiles only.
        if (toProfile)
            playerHeightJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);
        // Older scenes kept the method with Use Profile off; one that saved a method still counts as setting it.
        _sceneFirst.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromScene)
            fixedWorldScaleJSON.RestoreFromJSON(jc);

        // Older scenes saved with Use Profile off may also hold their creator's height; it is ignored.
        if (fromProfile)
            playerHeightJSON.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        fixedWorldScaleJSON.SetValToDefault();
        playerHeightJSON.SetValToDefault();
        _sceneFirst.ResetToDefault();
    }

    private void OnDestroy()
    {
        if (_navigationHologrid != null) SuperController.singleton.navigationHologrid = _navigationHologrid;
        if (_fakeHologrid != null) Destroy(_fakeHologrid.gameObject);
    }
}
