using System.Collections.Generic;
using UnityEngine;

public static class VisualCuesHelper
{
    public static List<GameObject> Cues = new List<GameObject>();

    public static GameObject Cross(Color color)
    {
        var go = new GameObject();
        Cues.Add(go);
        const float size = 0.2f;
        const float width = 0.005f;
        CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform.localScale = new Vector3(size, width, width);
        CreatePrimitive(go.transform, PrimitiveType.Cube, Color.green).transform.localScale = new Vector3(width, size, width);
        CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform.localScale = new Vector3(width, width, size);
        CreatePrimitive(go.transform, PrimitiveType.Sphere, color).transform.localScale = new Vector3(size / 8f, size / 8f, size / 8f);
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        return go;
    }

    public static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        Cues.Add(go);
        go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) {color = color, renderQueue = 4000};
        foreach (var c in go.GetComponents<Collider>())
        {
            c.enabled = false;
            Object.Destroy(c);
        }

        go.transform.parent = parent;
        return go;
    }

    public static LineRenderer CreateLine(Color color, float width, int points, bool useWorldSpace)
    {
        var go = new GameObject();
        Cues.Add(go);
        return CreateLine(go, color, width, points, useWorldSpace);
    }

    public static LineRenderer CreateLine(GameObject go, Color color, float width, int points, bool useWorldSpace)
    {
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = useWorldSpace;
        line.material = new Material(Shader.Find("Sprites/Default")) {renderQueue = 4000};
        line.widthMultiplier = width;
        line.colorGradient = new Gradient
        {
            colorKeys = new[] {new GradientColorKey(color, 0f), new GradientColorKey(color, 1f)}
        };
        line.positionCount = points;
        return line;
    }

    public static LineRenderer CreateEllipse(GameObject go, Color color, float width, int resolution = 32)
    {
        var line = CreateLine(go, color, width, resolution, false);
        return line;
    }

    public static void DrawEllipse(LineRenderer line, Vector2 radius)
    {
        for (var i = 0; i <= line.positionCount; i++)
        {
            var angle = i / (float) line.positionCount * 2.0f * Mathf.PI;
            var pointQuaternion = Quaternion.AngleAxis(90, Vector3.right);

            var pointPosition = new Vector3(radius.x * Mathf.Cos(angle), radius.y * Mathf.Sin(angle), 0.0f);
            pointPosition = pointQuaternion * pointPosition;

            line.SetPosition(i, pointPosition);
        }

        line.loop = true;
    }
}
