using System;
using UnityEngine;

public class MotionControllerWithCustomPossessPoint
{
    public string name;
    public Transform customPossessPoint;
    public Rigidbody customRigidbody;
    public Transform currentMotionControl { get; private set; }
    public string mappedControllerName { get; set; }
    private Func<Transform> _getMotionControl;
    private LineRenderer _lineRenderer;
    private Vector3 _baseOffset;
    private Vector3 _customOffset;

    public Vector3 baseOffset
    {
        get { return _baseOffset; }
        set { _baseOffset = value; SyncOffset(); }
    }

    public Vector3 customOffset
    {
        get { return _customOffset; }
        set { _customOffset = value; SyncOffset(); }
    }

    public Vector3 offset => _baseOffset + _customOffset;

    private void SyncOffset()
    {
        customPossessPoint.localPosition = offset;
        UpdateLineRenderer();
    }

    public Vector3 localEulerAngles
    {
        get { return customPossessPoint.localEulerAngles; }
        set
        {
            customPossessPoint.localEulerAngles = value;
            UpdateLineRenderer();
        }
    }



    public bool Connect()
    {
        currentMotionControl = _getMotionControl();
        if (currentMotionControl == null) return false;
        customPossessPoint.SetParent(currentMotionControl, false);
        SyncOffset();
        return true;
    }


    private void UpdateLineRenderer()
    {
        if (currentMotionControl == null) return;
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            customPossessPoint.InverseTransformPoint(currentMotionControl.position)
        });
    }

    public static MotionControllerWithCustomPossessPoint Create(string motionControlName, Func<Transform> getMotionControl)
    {
        var possessPointGameObject = new GameObject($"EmbodyPossessPoint_{motionControlName}");

        var rb = possessPointGameObject.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;

        // TODO: Optional
        var lineRenderer = VisualCuesHelper.CreateLine(possessPointGameObject, new Color(0.8f, 0.8f, 0.5f, 0.8f), 0.002f, 2, false, true);

        return new MotionControllerWithCustomPossessPoint
        {
            name = motionControlName,
            _getMotionControl = getMotionControl,
            customPossessPoint = possessPointGameObject.transform,
            customRigidbody = rb,
            _lineRenderer = lineRenderer
        };
    }
}
