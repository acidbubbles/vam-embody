using UnityEngine;

public class ControllerAnchorPoint
{
    public string Label { get; set; }
    public Rigidbody RigidBody { get; set; }
    public Vector3 PhysicalOffset { get; set; }
    public Vector3 PhysicalSize { get; set; }
    public Vector3 VirtualOffset { get; set; }
    public Vector3 VirtualSize { get; set; }
    public ControllerAnchorPointVisualCue VirtualCue { get; set; }
    public ControllerAnchorPointVisualCue PhysicalCue { get; set; }
    public bool Active { get; set; }
    public bool Locked { get; set; }

    public Vector3 GetWorldPosition()
    {
        return RigidBody.transform.position + RigidBody.transform.rotation * (VirtualOffset + PhysicalOffset);
    }

    public void Update()
    {
        if (VirtualCue != null)
        {
            VirtualCue.gameObject.SetActive(Active);
            VirtualCue.Update(VirtualOffset, VirtualSize);
        }

        if (PhysicalCue != null)
        {
            PhysicalCue.gameObject.SetActive(Active);
            PhysicalCue.Update(VirtualOffset + PhysicalOffset, PhysicalSize);
        }
    }
}
