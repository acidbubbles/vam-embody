using System.Linq;
using UnityEngine;

public class PersonMeasurements
{
    private const float _feetToGroundDistance = 0.059f;
    private const float _skeletonSumToStandHeightRatio = 0.945f;

    private readonly DAZBone[] _bones;
    private readonly DAZBone _hipBone;

    public PersonMeasurements(Atom containingAtom)
    {
        _bones = containingAtom.GetComponentsInChildren<DAZBone>();
        _hipBone = _bones.First(b => b.name == "hip");
    }

    public float MeasureHeight()
    {
        var footToHead = MeasureToHip("head") + ((MeasureToHip("lFoot") + MeasureToHip("rFoot")) / 2f);

        var lEye = _bones.First(eye => eye.name == "lEye").transform;
        var eyesToHeadDistance = _bones.First(b => b.name == "head").transform.InverseTransformPoint(lEye.position).y;

        var measure = footToHead + eyesToHeadDistance + _feetToGroundDistance;
         measure *= _skeletonSumToStandHeightRatio;

        return measure;
    }

    public float MeasureToHip(string boneName)
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
