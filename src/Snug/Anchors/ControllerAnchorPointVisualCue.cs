using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class ControllerAnchorPointVisualCue : IDisposable
{
    private const float _width = 0.005f;
    public readonly GameObject gameObject;
    private readonly Transform _xAxis;
    private readonly Transform _zAxis;
    private readonly Transform _frontHandle;
    private readonly Transform _leftHandle;
    private readonly Transform _rightHandle;
    private readonly LineRenderer _ellipse;

    public ControllerAnchorPointVisualCue(Transform parent, Color color)
    {
        var go = new GameObject();
        _xAxis = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform;
        _zAxis = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform;
        _frontHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
        _leftHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
        _rightHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
        _ellipse = VisualCuesHelper.CreateEllipse(go, color, _width);
        if (_ellipse == null) throw new NullReferenceException("Boom");
        gameObject = go;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
    }

    public void Update(Vector3 offset, Vector3 size)
    {
        gameObject.transform.localPosition = offset;

        _xAxis.localScale = new Vector3(size.x - _width * 2, _width * 0.25f, _width * 0.25f);
        _zAxis.localScale = new Vector3(_width * 0.25f, _width * 0.25f, size.z - _width * 2);
        _frontHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
        _frontHandle.transform.localPosition = Vector3.forward * size.z / 2;
        _leftHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
        _leftHandle.transform.localPosition = Vector3.left * size.x / 2;
        _rightHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
        _rightHandle.transform.localPosition = Vector3.right * size.x / 2;
        if (_ellipse == null) throw new NullReferenceException("Bam");
        VisualCuesHelper.DrawEllipse(_ellipse, new Vector2(size.x / 2f, size.z / 2f));
    }

    public void Dispose()
    {
        Object.Destroy(gameObject);
    }
}
