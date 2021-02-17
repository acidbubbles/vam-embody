using UnityEngine;

public class ControllerAnchorPoint
{
    public EmbodyScaleChangeReceiver scaleChangeReceiver { get; set; }
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
    public bool active { get; set; } = true;
    public bool locked { get; set; }
    public bool auto { get; set; } = true;

    public Vector3 GetInGameWorldPosition()
    {
        var rigidBodyTransform = bone.transform;
        // ReSharper disable once Unity.InefficientMultiplicationOrder
        return rigidBodyTransform.position + rigidBodyTransform.rotation * (inGameOffset * scaleChangeReceiver.scale);
    }

    public Vector3 GetAdjustedWorldPosition()
    {
        var rigidBodyTransform = bone.transform;
        return rigidBodyTransform.position + rigidBodyTransform.rotation * ((inGameOffset * scaleChangeReceiver.scale + realLifeOffset));
    }

    public void Update()
    {
        var scale = scaleChangeReceiver.scale;

        if (inGameCue != null)
        {
            inGameCue.gameObject.SetActive(active);
            inGameCue.Update(inGameOffset * scale, inGameSize * scale);
        }

        if (realLifeCue != null)
        {
            realLifeCue.gameObject.SetActive(active);
            realLifeCue.Update(inGameOffset * scale + realLifeOffset, realLifeSize);
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
