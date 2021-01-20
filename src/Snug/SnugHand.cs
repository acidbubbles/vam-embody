using UnityEngine;

public class SnugHand
{
    public bool active;
    public FreeControllerV3 controller;
    public MotionControllerWithCustomPossessPoint motionControl;
    public FreeControllerV3Snapshot snapshot { get; set; }
    public readonly Vector3[] visualCueLinePoints = new Vector3[2];
    public GameObject visualCueGameObject;
    public LineRenderer visualCueLineRenderer;

    public bool showCueLine
    {
        get
        {
            return visualCueLineRenderer != null;
        }
        set
        {
            if (value == true && visualCueGameObject == null)
            {
                visualCueGameObject = new GameObject();
                visualCueLineRenderer = visualCueGameObject.AddComponent<LineRenderer>();
                visualCueLineRenderer.useWorldSpace = true;
                var material = new Material(Shader.Find("Battlehub/RTHandles/VertexColor"));
                visualCueLineRenderer.material = material;
                visualCueLineRenderer.widthMultiplier = 0.0006f;
                visualCueLineRenderer.positionCount = 2;
            }
            else if (value == false && visualCueGameObject != null)
            {
                Object.Destroy(visualCueGameObject);
                visualCueGameObject = null;
                visualCueLineRenderer = null;
            }
        }
    }

    public void SyncCueLine()
    {
        if (visualCueLineRenderer == null) return;
        visualCueLineRenderer.SetPositions(visualCueLinePoints);
    }
}
