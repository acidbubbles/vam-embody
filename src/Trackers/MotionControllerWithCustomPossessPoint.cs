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
    public string mappedControllerName { get; set; }
    private Func<Transform> _getMotionControl;
    private LineRenderer _lineRenderer;
    private Vector3 _baseOffset;
    private Vector3 _baseOffsetRotation;
    private Vector3 _customOffset;
    private Vector3 _customOffsetRotation;
    private Vector3 _pointRotation;
    private bool _showLine;

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

    public bool showLine
    {
        get { return _showLine; }
        set { _showLine = value; CreateOrDestroyLineRenderer(); }
    }

    private void SyncOffset()
    {
        if (currentMotionControl == null) return;
        offsetTransform.localPosition = combinedOffset;
        offsetTransform.localEulerAngles = combinedOffsetRotation;
        possessPointTransform.localPosition = Quaternion.Euler(possessPointRotation) * combinedOffset - combinedOffset;
        possessPointTransform.localEulerAngles = possessPointRotation;
        SyncLineRenderer();
    }

    public bool SyncMotionControl()
    {
        currentMotionControl = _getMotionControl();
        if (currentMotionControl == null)
        {
            CreateOrDestroyLineRenderer();
            return false;
        }
        offsetTransform.SetParent(currentMotionControl, false);
        SyncOffset();
        CreateOrDestroyLineRenderer();
        return true;
    }

    private void CreateOrDestroyLineRenderer()
    {
        if (!_showLine || ReferenceEquals(currentMotionControl, null))
        {
            Object.Destroy(_lineRenderer);
            _lineRenderer = null;
            return;
        }

        if (_lineRenderer == null)
            _lineRenderer = VisualCuesHelper.CreateLine(possessPointTransform.gameObject, new Color(0.8f, 0.8f, 0.5f, 0.8f), 0.002f, 3, false, true);

        SyncLineRenderer();
    }

    private void SyncLineRenderer()
    {
        if (_lineRenderer == null) return;
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            possessPointTransform.InverseTransformPoint(offsetTransform.position),
            possessPointTransform.InverseTransformPoint(currentMotionControl.position),
        });
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
