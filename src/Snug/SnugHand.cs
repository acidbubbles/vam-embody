using UnityEngine;

public class SnugHand
{
    private GameObject _visualCueGameObject;
    private LineRenderer _visualCueLineRenderer;
    private FreeControllerV3 _controller;

    public bool active;
    public Rigidbody controllerRigidbody;
    public MotionControllerWithCustomPossessPoint motionControl;
    public FreeControllerV3Snapshot snapshot { get; set; }
    public readonly Vector3[] visualCueLinePoints = new Vector3[2];

    public FreeControllerV3 controller
    {
        get { return _controller; }
        set { _controller = value; controllerRigidbody = value.GetComponent<Rigidbody>(); }
    }

    public bool showCueLine
    {
        get
        {
            return _visualCueLineRenderer != null;
        }
        set
        {
            if (value && _visualCueGameObject == null)
            {
                _visualCueGameObject = new GameObject();
                _visualCueLineRenderer = _visualCueGameObject.AddComponent<LineRenderer>();
                _visualCueLineRenderer.useWorldSpace = true;
                _visualCueLineRenderer.startColor = Color.green;
                _visualCueLineRenderer.endColor = Color.red;
                var material = new Material(Shader.Find("Battlehub/RTHandles/VertexColor"));
                _visualCueLineRenderer.material = material;
                _visualCueLineRenderer.widthMultiplier = 0.0006f;
                _visualCueLineRenderer.positionCount = 2;
            }
            else if (value == false && _visualCueGameObject != null)
            {
                Object.Destroy(_visualCueGameObject);
                _visualCueGameObject = null;
                _visualCueLineRenderer = null;
            }
        }
    }

    public void SyncCueLine()
    {
        if (_visualCueLineRenderer == null) return;
        _visualCueLineRenderer.SetPositions(visualCueLinePoints);
    }
}
