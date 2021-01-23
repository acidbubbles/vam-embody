using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class MotionControllerWithCustomPossessPoint
{
    public string name;
    public Transform offsetTransform;
    public Transform possessPointTransform;
    public Rigidbody customRigidbody;
    public Transform currentMotionControl { get; private set; }
    public bool enabled = true;
    public string mappedControllerName { get; set; }
    private Func<Transform> _getMotionControl;
    private OffsetPreview _offsetPreview;
    private Vector3 _baseOffset;
    private Vector3 _baseOffsetRotation;
    private Vector3 _customOffset;
    private Vector3 _customOffsetRotation;
    private Vector3 _pointRotation;
    private bool _showPreview;

    public Vector3 baseOffset
    {
        get { return _baseOffset; }
        set { _baseOffset = value; SyncOffset(); }
    }

    public Vector3 baseOffsetRotation
    {
        get { return _baseOffsetRotation; }
        set { _baseOffsetRotation = value; SyncOffset(); }
    }

    public Vector3 customOffset
    {
        get { return _customOffset; }
        set { _customOffset = value; SyncOffset(); }
    }

    public Vector3 customOffsetRotation
    {
        get { return _customOffsetRotation; }
        set { _customOffsetRotation = value; SyncOffset(); }
    }

    public Vector3 possessPointRotation
    {
        get { return _pointRotation; }
        set { _pointRotation = value; SyncOffset(); }
    }

    public Vector3 combinedOffset => _baseOffset + _customOffset;
    public Vector3 combinedOffsetRotation => _baseOffsetRotation + _customOffsetRotation;

    public bool showPreview
    {
        get { return _showPreview; }
        set { _showPreview = value; SyncMotionControl(); }
    }

    private void SyncOffset()
    {
        if (currentMotionControl == null) return;
        offsetTransform.localPosition = Vector3.zero;
        offsetTransform.localEulerAngles = _pointRotation;
        possessPointTransform.localPosition = combinedOffset;
        possessPointTransform.localEulerAngles = combinedOffsetRotation;
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
        offsetTransform.SetParent(currentMotionControl, false);
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
            go.transform.SetParent(possessPointTransform, false);
            _offsetPreview = go.gameObject.AddComponent<OffsetPreview>();
        }

        _offsetPreview.offsetTransform = offsetTransform;
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
        _offsetPreview.Sync();
    }

    public static MotionControllerWithCustomPossessPoint Create(string motionControlName, Func<Transform> getMotionControl)
    {
        var offsetPointGameObject = new GameObject($"EmbodyPossessOffsetPoint_{motionControlName}");
        var possessPointGameObject = new GameObject($"EmbodyPossessPoint_{motionControlName}");

        var rb = possessPointGameObject.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;

        possessPointGameObject.transform.SetParent(offsetPointGameObject.transform, false);

        return new MotionControllerWithCustomPossessPoint
        {
            name = motionControlName,
            _getMotionControl = getMotionControl,
            offsetTransform = offsetPointGameObject.transform,
            possessPointTransform = possessPointGameObject.transform,
            customRigidbody = rb,
        };
    }
}
