using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utilities
{
    public static IEnumerator CreateMirror(IEyeTargetModule eyeTarget, Atom containingAtom)
    {
        var uid = CreateUid("Mirror");
        var enumerator = SuperController.singleton.AddAtomByType("Glass", uid, true);
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
        var atom = SuperController.singleton.GetAtomByUid(uid);
        if (atom == null) throw new NullReferenceException("Mirror did not spawn");
        atom.collisionEnabled = false;
        // ReSharper disable once Unity.NoNullCoalescing
        var head = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == "head")?.transform ?? containingAtom.mainController.transform;
        var atomControl = atom.mainController.transform;
        var rotation = new Vector3(0, atomControl.eulerAngles.y, 0);
        atomControl.position = head.position - (new Vector3(0, head.position.y, 0) / 2f) + Quaternion.Euler(rotation) * (Vector3.forward * 0.5f);
        atomControl.eulerAngles = rotation;
        atomControl.Rotate(Vector3.right, -90);

        var scale = atom.GetStorableByID("scale") as SetTransformScaleIndependent;
        if (scale != null)
        {
            scale.SetFloatParamValue("scaleX", 1.4f);
            scale.SetFloatParamValue("scaleZ", 2f);
        }

        var reflection = atom.GetStorableByID("MirrorRender") as MirrorReflection;
        if (reflection != null)
        {
            reflection.reflectionOpacity = 1f;
            reflection.surfaceTexturePower = 0.001f;
            reflection.specularIntensity = 0.2f;
            reflection.textureSize = 2048;
            reflection.SetReflectionColor(new HSVColor {V = 1});
        }

        yield return 0;

        if (eyeTarget.activeJSON.val)
            eyeTarget.Rescan();
    }

    private static string CreateUid(string source)
    {
        var uids = new HashSet<string>(SuperController.singleton.GetAtomUIDs());
        if (!uids.Contains(source)) return source;
        source += "#";
        for (var i = 1; i < 1000; i++)
        {
            var uid = source + i;
            if (!uids.Contains(uid)) return uid;
        }
        return source + Guid.NewGuid();
    }
}
