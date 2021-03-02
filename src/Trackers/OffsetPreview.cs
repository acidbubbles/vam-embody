using Leap.Unity;
using UnityEngine;

public class OffsetPreview : MonoBehaviour
{
    private const float _highlightedAlpha = 1f;
    private const float _normalAlpha = 0.3f;
    public Transform currentMotionControl;
    private LineRenderer _lineRenderer;
    private Transform _controllerPreview;
    private Transform _motionControlPreview;

    public void Awake()
    {
        _lineRenderer = CreateLine();
        _controllerPreview = CreateAxisIndicator(new Color(0.6f, 0.8f, 0.6f, _normalAlpha));
        _motionControlPreview = CreateAxisIndicator(new Color(0.4f, 0.2f, 0.2f, _normalAlpha));
    }

    public void Sync(bool highlighted)
    {
        var motionControlPosition = currentMotionControl.position;
        _lineRenderer.SetPositions(new[]
        {
            Vector3.zero,
            transform.InverseTransformPoint(motionControlPosition),
        });
        if (_motionControlPreview != null)
        {
            _motionControlPreview.SetPositionAndRotation(motionControlPosition, currentMotionControl.rotation);
            foreach (var renderer in _motionControlPreview.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.material;
                if (material == null) continue;
                material.color = renderer.material.color.WithAlpha(highlighted ? _highlightedAlpha : _normalAlpha);
            }
        }
        if (_controllerPreview != null)
        {
            foreach (var renderer in _controllerPreview.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.material;
                if (material == null) continue;
                material.color = material.color.WithAlpha(highlighted ? _highlightedAlpha : _normalAlpha);
            }
        }
    }

    private Transform CreateAxisIndicator(Color color)
    {
        var indicator = CreatePrimitive(transform, PrimitiveType.Cube, color, 0.022f);

        var axisScale = 0.35f;
        var axisOffset = axisScale / 2f;
        var x = CreatePrimitive(indicator.transform, PrimitiveType.Cube, new Color(1f, 0f, 0f, _normalAlpha), axisScale);
        x.transform.localPosition = new Vector3(0.5f + axisOffset, 0f, 0f);

        var y = CreatePrimitive(indicator.transform, PrimitiveType.Cube, new Color(0f, 1f, 0f, _normalAlpha), axisScale);
        y.transform.localPosition = new Vector3(0f, 0.5f + axisOffset, 0f);

        var z = CreatePrimitive(indicator.transform, PrimitiveType.Cube, new Color(0f, 0f, 1f, _normalAlpha), axisScale);
        z.transform.localPosition = new Vector3(0f, 0f, 0.5f + axisOffset);

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

    private LineRenderer CreateLine()
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
