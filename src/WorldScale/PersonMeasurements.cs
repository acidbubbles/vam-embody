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
        var measure = Distance(
            Get("head"),
            Get("neck"),
            Get("chest"),
            Get("abdomen2"),
            Get("abdomen"),
            Get("hip"),
            Get("pelvis"),
            Get("rThigh"),
            Get("rShin"),
            Get("rFoot")
        );

        var eyes = _containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var lEye = eyes.First(eye => eye.name == "lEye").transform;
        var eyesToHeadDistance = Mathf.Abs(lEye.position.y - Get("head").position.y);

        measure += eyesToHeadDistance + _feetToGroundDistance;
        measure *= _skeletonSumToStandHeightRatio;

        return measure;
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
