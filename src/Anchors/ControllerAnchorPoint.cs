using UnityEngine;

public class ControllerAnchorPoint
{
    public string Label { get; set; }
    public Rigidbody RigidBody { get; set; }
    public Vector3 RealLifeOffset { get; set; }
    public Vector3 RealLifeSize { get; set; }
    public Vector3 InGameOffset { get; set; }
    public Vector3 InGameSize { get; set; }
    public ControllerAnchorPointVisualCue VirtualCue { get; set; }
    public ControllerAnchorPointVisualCue PhysicalCue { get; set; }
    public bool Active { get; set; }
    public bool Locked { get; set; }

    public Vector3 GetWorldPosition()
    {
        return RigidBody.transform.position + RigidBody.transform.rotation * (InGameOffset + RealLifeOffset);
    }

    public void Update()
    {
        if (VirtualCue != null)
        {
            VirtualCue.gameObject.SetActive(Active);
            VirtualCue.Update(InGameOffset, InGameSize);
        }

        if (PhysicalCue != null)
        {
            PhysicalCue.gameObject.SetActive(Active);
            PhysicalCue.Update(InGameOffset + RealLifeOffset, RealLifeSize);
        }
    }
}
