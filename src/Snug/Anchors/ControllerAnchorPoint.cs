using UnityEngine;

public class ControllerAnchorPoint
{
    public string id { get; set; }
    public string label { get; set; }
    public Transform bone { get; set; }
    public Vector3 realLifeOffsetDefault { get; set; }
    public Vector3 realLifeOffset { get; set; }
    public Vector3 realLifeSizeDefault { get; set; }
    public Vector3 realLifeSize { get; set; }
    public Vector3 inGameOffsetDefault { get; set; }
    public Vector3 inGameOffset { get; set; }
    public Vector3 inGameSizeDefault { get; set; }
    public Vector3 inGameSize { get; set; }
    public ControllerAnchorPointVisualCue inGameCue { get; set; }
    public ControllerAnchorPointVisualCue realLifeCue { get; set; }
    public bool active { get; set; }
    public bool locked { get; set; }

    public Vector3 GetInGameWorldPosition()
    {
        var rigidBodyTransform = bone.transform;
        return rigidBodyTransform.position + rigidBodyTransform.rotation * inGameOffset;
    }

    public Vector3 GetAdjustedWorldPosition()
    {
        var rigidBodyTransform = bone.transform;
        return rigidBodyTransform.position + rigidBodyTransform.rotation * (inGameOffset + realLifeOffset);
    }

    public void Update()
    {
        if (inGameCue != null)
        {
            inGameCue.gameObject.SetActive(active);
            inGameCue.Update(inGameOffset, inGameSize);
        }

        if (realLifeCue != null)
        {
            realLifeCue.gameObject.SetActive(active);
            realLifeCue.Update(inGameOffset + realLifeOffset, realLifeSize);
        }
    }

    public void InitFromDefault()
    {
        inGameOffset = inGameOffsetDefault;
        inGameSize = inGameSizeDefault;
        realLifeOffset = realLifeOffsetDefault;
        realLifeSize = realLifeSizeDefault;
    }
}
