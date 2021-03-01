using System;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

public class MotionControllerWithCustomPossessPoint
{
    public string name;
    public Transform trackerPointTransform;
    public Transform controllerPointTransform;
    public Rigidbody controllerPointRB;
    public bool enabled = true;
    public bool controlRotation = true;
    public bool useLeapPositioning;
    public bool fingersTracking = true;
    public string mappedControllerName;
    public Transform currentMotionControl { get; private set; }
    private Func<Transform> _getMotionControl;
    private Action<MotionControllerWithCustomPossessPoint> _configure;
    private OffsetPreview _offsetPreview;
    private Vector3 _offsetControllerBase;
    private Vector3 _rotateControllerBase;
    private Vector3 _offsetControllerCustom;
    private Vector3 _rotateControllerCustom;
    private Vector3 _rotateAroundTracker;
    private bool _showPreview;
    private bool _highlighted;

    public Vector3 offsetControllerBase
    {
        get { return _offsetControllerBase; }
        set { _offsetControllerBase = value; }
    }

    public Vector3 rotateControllerBase
    {
        get { return _rotateControllerBase; }
        set { _rotateControllerBase = value; }
    }

    public Vector3 offsetControllerCustom
    {
        get { return _offsetControllerCustom; }
        set { _offsetControllerCustom = value; SyncOffset(); }
    }

    public Vector3 rotateControllerCustom
    {
        get { return _rotateControllerCustom; }
        set { _rotateControllerCustom = value; SyncOffset(); }
    }

    public Vector3 rotateAroundTracker
    {
        get { return _rotateAroundTracker; }
        set { _rotateAroundTracker = value; SyncOffset(); }
    }

    public Vector3 offsetControllerCombined => _offsetControllerBase + _offsetControllerCustom;
    public Vector3 rotateControllerCombined => _rotateControllerBase + _rotateControllerCustom;

    public bool showPreview
    {
        get { return _showPreview; }
        set { _showPreview = value; SyncMotionControl(); }
    }

    public bool highlighted
    {
        get { return _highlighted; }
        set { _highlighted = value; SyncOffsetPreview(); }
    }

    private void SyncOffset()
    {
        if (currentMotionControl == null) return;
        trackerPointTransform.localPosition = Vector3.zero;
        trackerPointTransform.localEulerAngles = _rotateAroundTracker;
        controllerPointTransform.localPosition = offsetControllerCombined;
        controllerPointTransform.localEulerAngles = rotateControllerCombined;
        SyncOffsetPreview();
    }

    public bool SyncMotionControl()
    {
        // TODO: Crash when moving from debug object to actual motion control
        currentMotionControl = _getMotionControl();
        if (currentMotionControl == null || !currentMotionControl.gameObject.activeInHierarchy)
        {
            DestroyOffsetPreview();
            return false;
        }
        _configure?.Invoke(this);
        trackerPointTransform.SetParent(currentMotionControl, false);
        if(!_showPreview)
            DestroyOffsetPreview();
        else
            CreatOffsetPreview();
        SyncOffset();
        return true;
    }

    private void CreatOffsetPreview()
    {
        if (_offsetPreview == null)
        {
            var go = new GameObject("EmbodyOffsetPreview_" + name);
            go.transform.SetParent(controllerPointTransform, false);
            _offsetPreview = go.gameObject.AddComponent<OffsetPreview>();
        }

        _offsetPreview.currentMotionControl = currentMotionControl;
    }

    private void DestroyOffsetPreview()
    {
        if (_offsetPreview == null) return;
        Object.Destroy(_offsetPreview.gameObject);
        _offsetPreview = null;
    }

    private void SyncOffsetPreview()
    {
        if (_offsetPreview == null) return;
        _offsetPreview.Sync(_highlighted);
    }

    public static MotionControllerWithCustomPossessPoint Create(string motionControlName, Func<Transform> getMotionControl, Action<MotionControllerWithCustomPossessPoint> configure)
    {
        var trackerPointGO = new GameObject($"EmbodyTrackerPoint_{motionControlName}");
        var controllerPointGO = new GameObject($"EmbodyControllerPoint_{motionControlName}");

        var rb = controllerPointGO.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;

        controllerPointGO.transform.SetParent(trackerPointGO.transform, false);

        return new MotionControllerWithCustomPossessPoint
        {
            name = motionControlName,
            _getMotionControl = getMotionControl,
            _configure = configure,
            trackerPointTransform = trackerPointGO.transform,
            controllerPointTransform = controllerPointGO.transform,
            controllerPointRB = rb,
        };
    }

    public JSONClass GetJSON()
    {
        var motionControlJSON = new JSONClass
        {
            {"OffsetPosition", offsetControllerCustom.ToJSON()},
            {"OffsetRotation", rotateControllerCustom.ToJSON()},
            {"PossessPointRotation", rotateAroundTracker.ToJSON()},
            {"Controller", mappedControllerName},
            {"Enabled", enabled ? "true" : "false"},
            {"ControlRotation", controlRotation ? "true" : "false"},
            {"UseLeap", useLeapPositioning ? "true" : "false"},
            {"FingersTracking", fingersTracking ? "true" : "false"},
        };
        return motionControlJSON;
    }

    public void RestoreFromJSON(JSONNode jc)
    {
        offsetControllerCustom = jc["OffsetPosition"].AsObject.ToVector3(Vector3.zero);
        rotateControllerCustom = jc["OffsetRotation"].AsObject.ToVector3(Vector3.zero);
        rotateAroundTracker = jc["PossessPointRotation"].AsObject.ToVector3(Vector3.zero);
        mappedControllerName = jc["Controller"].Value;
        enabled = jc["Enabled"].Value != "false";
        controlRotation = jc["ControlRotation"].Value != "false";
        useLeapPositioning = jc["UseLeap"].Value == "true";
        fingersTracking = jc["FingersTracking"].Value != "false";
        if (mappedControllerName == "") mappedControllerName = null;
    }

    public void ResetToDefault()
    {
        if (!MotionControlNames.IsHeadOrHands(name))
            mappedControllerName = null;
        controlRotation = true;
        useLeapPositioning = false;
        fingersTracking = true;
        offsetControllerCustom = Vector3.zero;
        rotateControllerCustom = Vector3.zero;
        rotateAroundTracker = Vector3.zero;
        enabled = true;
    }
}
