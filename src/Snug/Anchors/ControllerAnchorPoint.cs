using UnityEngine;

public class ControllerAnchorPoint
{
    public EmbodyScaleChangeReceiver scaleChangeReceiver;
    public string id;
    public string label;
    public Transform bone;
    public Transform altBone;
    public Vector3 realLifeOffsetDefault;
    public Vector3 realLifeOffset;
    public Vector3 realLifeSizeDefault;
    public Vector3 realLifeSize;
    public Vector3 inGameOffsetDefault;
    public Vector3 inGameOffset;
    public Vector3 inGameSizeDefault;
    public Vector3 inGameSize;
    public ControllerAnchorPointVisualCue inGameCue;
    public ControllerAnchorPointVisualCue realLifeCue;
    public bool active = true;
    public bool auto = true;
    public bool locked;
    public bool floor;

    public Vector3 GetInGameWorldPosition()
    {
        var rigidBodyTransform = bone.transform;
        // ReSharper disable once Unity.InefficientMultiplicationOrder
        return rigidBodyTransform.position + rigidBodyTransform.rotation * (inGameOffset * scaleChangeReceiver.scale);
    }

    public Vector3 GetAdjustedWorldPosition()
    {
        return GetAdjustedWorldPosition(bone.transform);
    }

    public Vector3 GetAdjustedWorldPosition(Transform rigidBodyTransform)
    {
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
