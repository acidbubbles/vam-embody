using UnityEngine;
using UnityEngine.Rendering;

public class OffsetPreview : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    public Transform offsetTransform;
    public Transform currentMotionControl;
    private Transform _motionControlPreview;

    public void Awake()
    {
        _lineRenderer = VisualCuesHelper.CreateLine(gameObject, new Color(0.8f, 0.8f, 0.5f, 0.8f), 0.002f, 3, false, true);
        var possessPointPreview = CreatePrimitive(PrimitiveType.Cube, Color.cyan, 0.008f).transform;
        _motionControlPreview = CreatePrimitive(PrimitiveType.Cube, Color.green, 0.008f).transform;
    }

    public void Sync()
    {
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            transform.InverseTransformPoint(offsetTransform.position),
            transform.InverseTransformPoint(currentMotionControl.position),
        });
        _motionControlPreview.SetPositionAndRotation(currentMotionControl.position, currentMotionControl.rotation);
    }

    public GameObject CreatePrimitive(PrimitiveType type, Color color, float scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(scale, scale, scale);
        var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
        material.SetFloat("_Scale", scale);
        material.SetFloat("_Offset", 1f);
        material.SetFloat("_MinAlpha", 1f);
        material.enableInstancing = true;
        material.color = color;
        go.GetComponent<Renderer>().material = material;
        foreach (var c in go.GetComponents<Collider>())
        {
            c.enabled = false;
            Destroy(c);
        }

        return go;
    }
}
