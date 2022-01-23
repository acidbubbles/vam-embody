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
    public bool controlPosition = true;
    public bool controlRotation = true;
    public bool useLeapPositioning;
    public bool fingersTracking = true;
    public string defaultControllerName;
    public string mappedControllerName;
    public bool keepCurrentPhysicsHoldStrength;
    public Transform currentMotionControl { get; private set; }
    private Func<Transform> _getMotionControl;
    private Action<MotionControllerWithCustomPossessPoint> _configure;
    private OffsetPreview _offsetPreview;
    private Vector3 _offsetControllerBase;
    private Vector3 _rotateControllerBase;
    private Vector3 _offsetControllerCustom;
    private Vector3 _rotateControllerCustom;
    private Vector3 _rotateAroundTrackerCustom;
    private bool _showPreview;
    private bool _highlighted;

    public Vector3 offsetControllerBase
    {
        get { return _offsetControllerBase; }
        set { _offsetControllerBase = value; SyncOffset(); }
    }

    public Vector3 rotateControllerBase
    {
        get { return _rotateControllerBase; }
        set { _rotateControllerBase = value; SyncOffset(); }
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

    public Vector3 rotateAroundTrackerCustom
    {
        get { return _rotateAroundTrackerCustom; }
        set { _rotateAroundTrackerCustom = value; SyncOffset(); }
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
        if (trackerPointTransform == null) return;
        trackerPointTransform.localPosition = Vector3.zero;
        trackerPointTransform.localEulerAngles = _rotateAroundTrackerCustom;
        controllerPointTransform.localPosition = offsetControllerCombined;
        controllerPointTransform.localEulerAngles = rotateControllerCombined;
        SyncOffsetPreview();
    }

    public bool SyncMotionControl()
    {
        var previousMotionControl = currentMotionControl;
        currentMotionControl = _getMotionControl();
        if (currentMotionControl == null)
        {
            DestroyChildren();
            return false;
        }

        if (previousMotionControl == null)
        {
            InitializeMotionControlChildren();
        }
        else if (previousMotionControl != currentMotionControl)
        {
            DestroyChildren();
            InitializeMotionControlChildren();
        }
        _configure?.Invoke(this);

        var hasOffsetPreview = _offsetPreview != null;
        if(!_showPreview && hasOffsetPreview)
            DestroyOffsetPreview();
        else if(showPreview && !hasOffsetPreview)
            CreatOffsetPreview();

        SyncOffset();

        return true;
    }

    private void DestroyChildren()
    {
        if (trackerPointTransform != null)
        {
            Object.Destroy(trackerPointTransform.gameObject);
            trackerPointTransform = null;
        }
        if(controllerPointTransform != null)
        {
            Object.Destroy(controllerPointTransform.gameObject);
            controllerPointTransform = null;
            controllerPointRB = null;
        }
        DestroyOffsetPreview();
    }

    private void DestroyOffsetPreview()
    {
        if (_offsetPreview == null) return;
        Object.Destroy(_offsetPreview.gameObject);
        _offsetPreview = null;
    }

    private void InitializeMotionControlChildren()
    {
        var trackerPointGO = new GameObject($"EmbodyTrackerPoint_{name}");
        trackerPointTransform = trackerPointGO.transform;
        trackerPointTransform.SetParent(currentMotionControl, false);
        var controllerPointGO = new GameObject($"EmbodyControllerPoint_{name}");
        controllerPointTransform = controllerPointGO.transform;
        controllerPointTransform.SetParent(trackerPointTransform, false);

        controllerPointRB = controllerPointGO.AddComponent<Rigidbody>();
        controllerPointRB.interpolation = RigidbodyInterpolation.None;
        controllerPointRB.isKinematic = true;
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
        _offsetPreview.Sync(_highlighted);
    }

    private void SyncOffsetPreview()
    {
        if (_offsetPreview == null) return;
        _offsetPreview.Sync(_highlighted);
    }

    public static MotionControllerWithCustomPossessPoint Create(string motionControlName, Func<Transform> getMotionControl, Action<MotionControllerWithCustomPossessPoint> configure)
    {
        return new MotionControllerWithCustomPossessPoint
        {
            name = motionControlName,
            _getMotionControl = getMotionControl,
            _configure = configure
        };
    }

    public JSONClass GetJSON(bool exportDefaults)
    {
        if (!exportDefaults)
        {
            return new JSONClass
            {
                {"Enabled", enabled ? "true" : "false"},
            };
        }

        return new JSONClass
        {
            {"Enabled", enabled ? "true" : "false"},
            {"OffsetPosition", offsetControllerCustom.ToJSON()},
            {"OffsetRotation", rotateControllerCustom.ToJSON()},
            {"PossessPointRotation", rotateAroundTrackerCustom.ToJSON()},
            {"Controller", mappedControllerName},
            {"ControlRotation", controlRotation ? "true" : "false"},
            {"ControlPosition", controlPosition ? "true" : "false"},
            {"UseLeap", useLeapPositioning ? "true" : "false"},
            {"FingersTracking", fingersTracking ? "true" : "false"},
            {"KeepCurrentPhysicsHoldStrength", keepCurrentPhysicsHoldStrength ? "true" : "false"},
        };
    }

    public void RestoreFromJSON(JSONNode jc, bool importDefaults)
    {
        enabled = jc["Enabled"].Value != "false";
        if (mappedControllerName == "") mappedControllerName = defaultControllerName;
        if (!importDefaults) return;
        offsetControllerCustom = jc["OffsetPosition"].AsObject.ToVector3(Vector3.zero);
        rotateControllerCustom = jc["OffsetRotation"].AsObject.ToVector3(Vector3.zero);
        rotateAroundTrackerCustom = jc["PossessPointRotation"].AsObject.ToVector3(Vector3.zero);
        mappedControllerName = jc["Controller"].Value;
        controlRotation = jc["ControlRotation"].Value != "false";
        controlPosition = jc["ControlPosition"].Value != "false";
        useLeapPositioning = jc["UseLeap"].Value == "true";
        fingersTracking = jc["FingersTracking"].Value != "false";
        keepCurrentPhysicsHoldStrength = jc["KeepCurrentPhysicsHoldStrength"].Value == "true";
    }

    public void ResetToDefault(bool onlyPersonalData = false)
    {
        offsetControllerCustom = Vector3.zero;
        rotateControllerCustom = Vector3.zero;
        rotateAroundTrackerCustom = Vector3.zero;
        if (onlyPersonalData) return;
        enabled = true;
        mappedControllerName = defaultControllerName;
        controlRotation = true;
        controlPosition = true;
        useLeapPositioning = false;
        fingersTracking = true;
        keepCurrentPhysicsHoldStrength = false;
    }
}
