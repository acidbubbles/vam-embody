using System.Collections.Generic;
using UnityEngine;

public class SnugHand
{
    public bool active;
    public FreeControllerV3 controller;
    public readonly Vector3[] visualCueLinePoints = new Vector3[2];
    public LineRenderer visualCueLineRenderer;
    public readonly List<GameObject> visualCueLinePointIndicators = new List<GameObject>();
    public MotionControllerWithCustomPossessPoint motionControl;
    public FreeControllerV3Snapshot snapshot { get; set; }
}
