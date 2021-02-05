using System.Linq;
using UnityEngine;

public class PersonMeasurements
{
    private const float _feetToGroundDistance = 0.059f;
    private const float _skeletonSumToStandHeightRatio = 0.863f;

    private readonly Atom _containingAtom;

    public PersonMeasurements(Atom containingAtom)
    {
        _containingAtom = containingAtom;
    }

    public float MeasureHeight()
    {
        var bones = _containingAtom.GetComponentsInChildren<DAZBone>();
        var headBone = bones.First(b => b.name == "head");
        var pelvisBone = bones.First(b => b.name == "pelvis");
        var lFootBone = bones.First(b => b.name == "lFoot");
        var rFootBone = bones.First(b => b.name == "rFoot");
        var footToHead = Measure(headBone, pelvisBone) + (Measure(lFootBone, pelvisBone) + Measure(rFootBone, pelvisBone) / 2f);

        var eyes = _containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.First(eye => eye.name == "lEye").transform;
        var eyesToHeadDistance = headBone.transform.InverseTransformPoint(lEye.position).y;

        var measure = footToHead + eyesToHeadDistance + _feetToGroundDistance;
        // measure *= _skeletonSumToStandHeightRatio;

        return measure;
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

    private static float Distance(params Rigidbody[] rigidbodies)
    {
        var total = 0f;
        for (var i = 0; i < rigidbodies.Length - 1; i++)
        {
            total += Vector3.Distance(rigidbodies[i].position, rigidbodies[i + 1].position);
        }
        return total;
    }

    private Rigidbody Get(string rbName)
    {
        return _containingAtom.rigidbodies.First(rb => rb.name == rbName);
    }
}
