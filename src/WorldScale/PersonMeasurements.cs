using System.Linq;
using UnityEngine;

public class PersonMeasurements
{
    private readonly DAZBone[] _bones;
    private readonly DAZBone _hipBone;

    public PersonMeasurements(EmbodyContext context)
    {
        _bones = context.bones;
        _hipBone = _bones.First(b => b.name == "hip");
    }

    public float MeasureHeight()
    {
        const float hipToHeadHeightRatio = 0.926f;

        var floorToHip = MeasureHipHeight();
        var upper = MeasureToHip("head") * hipToHeadHeightRatio;
        var floorToHead = floorToHip + upper;

        var lEye = _bones.First(eye => eye.name == "lEye").transform;
        var eyesToHeadDistance = _bones.First(b => b.name == "head").transform.InverseTransformPoint(lEye.position).y;

        var measure = floorToHead + eyesToHeadDistance;
        return measure;
    }

    public float MeasureHipHeight()
    {
        const float feetToHipHeightRatio = 0.992f;
        const float feetOffset = 0.054f;

        var footToHip = ((MeasureToHip("lFoot") + MeasureToHip("rFoot")) / 2f) * feetToHipHeightRatio;
        return footToHip + feetOffset;
    }

    private float MeasureToHip(string boneName)
    {
        var from = _bones.First(b => b.name == boneName);
        return Measure(from, _hipBone);
    }

    private static float Measure(DAZBone from, DAZBone to)
    {
        var bone = from;
        var length = 0f;
        while (true)
        {
            if (bone.parentBone == to || ReferenceEquals(bone.parentBone, null))
                break;

            length += Vector3.Distance(bone.parentBone.transform.position, bone.transform.position);
            bone = bone.parentBone;
        }
        return length;
    }
}
