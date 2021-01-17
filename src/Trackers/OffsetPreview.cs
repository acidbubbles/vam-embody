using UnityEngine;

public class OffsetPreview : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    public Transform offsetTransform;
    public Transform currentMotionControl;

    public void Start()
    {
        _lineRenderer = VisualCuesHelper.CreateLine(gameObject, new Color(0.8f, 0.8f, 0.5f, 0.8f), 0.002f, 3, false, true);
        Sync();
    }

    public void Sync()
    {
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            transform.InverseTransformPoint(offsetTransform.position),
            transform.InverseTransformPoint(currentMotionControl.position),
        });
    }
}
