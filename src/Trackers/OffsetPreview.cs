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
        _lineRenderer = CreateLine();
        CreateAxisIndicator(new Color(0.6f, 0.8f, 0.6f));
        _motionControlPreview = CreateAxisIndicator(new Color(0.4f, 0.2f, 0.2f));
    }

    public void Sync()
    {
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            transform.InverseTransformPoint(currentMotionControl.position),
        });
        _motionControlPreview.SetPositionAndRotation(currentMotionControl.position, currentMotionControl.rotation);
    }

    private Transform CreateAxisIndicator(Color color)
    {
        var indicator = CreatePrimitive(transform, PrimitiveType.Cube, color, 0.008f);

        var x = CreatePrimitive(indicator.transform, PrimitiveType.Cube, Color.red, 0.25f);
        x.transform.localPosition = new Vector3(0.5f + 0.125f, 0f, 0f);

        var y = CreatePrimitive(indicator.transform, PrimitiveType.Cube, Color.green, 0.25f);
        y.transform.localPosition = new Vector3(0f, 0.5f + 0.125f, 0f);

        var z = CreatePrimitive(indicator.transform, PrimitiveType.Cube, Color.blue, 0.25f);
        z.transform.localPosition = new Vector3(0f, 0f, 0.5f + 0.125f);

        return indicator.transform;
    }

    private static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color, float scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, scale);
        var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
        material.SetFloat("_Scale", go.transform.lossyScale.x);
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

    public LineRenderer CreateLine()
    {
        var line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
            var material = new Material(Shader.Find("Battlehub/RTHandles/VertexColor"));
            line.material = material;
        line.widthMultiplier = 0.0006f;
        line.positionCount = 2;
        return line;
    }
}
