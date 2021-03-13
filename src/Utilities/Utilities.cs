using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utilities
{
    public static void ResetToDefaults(EmbodyContext context)
    {
        context.embody.activeJSON.val = false;
        context.automation.ResetToDefault();
        context.worldScale.ResetToDefault();
        context.hideGeometry.ResetToDefault();
        context.offsetCamera.ResetToDefault();
        context.passenger.ResetToDefault();
        context.trackers.ResetToDefault();
        context.snug.ResetToDefault();
        context.eyeTarget.ResetToDefault();
    }

    public static void ReCenterPose(Atom atom)
    {
        var controllers = atom.freeControllers.Where(fc => fc.name.EndsWith("Control")).Where(c => c.currentPositionState != FreeControllerV3.PositionState.Off).ToList();
        if (controllers.Count == 0) return;
        var center = controllers.Aggregate(Vector3.zero, (a, c) => a += c.control.position) / controllers.Count;
        var offset = center - atom.mainController.control.position;
        offset.y = 0;
        foreach (var controller in controllers)
            controller.control.position -= offset;
    }

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
        var headPosition = head.position;
        var headRotation = Quaternion.LookRotation(Vector3.Scale(head.forward, new Vector3(1f, 0f, 1f)), Vector3.up);
        const float mirrorDistance = 0.8f;
        atomControl.position = headPosition - (new Vector3(0, headPosition.y, 0) / 2f) + headRotation * (Vector3.forward * mirrorDistance);
        atomControl.rotation = headRotation;
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
            reflection.reflectionOpacity = 0.92f;
            reflection.surfaceTexturePower = 0.001f;
            reflection.specularIntensity = 0.05f;
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
        for (var i = 2; i < 1000; i++)
        {
            var uid = source + i;
            if (!uids.Contains(uid)) return uid;
        }
        return source + Guid.NewGuid();
    }

    // ReSharper disable once Unity.NoNullPropagation Unity.NoNullCoalescing

    public static void StartRecord(EmbodyContext context)
    {
        context.embody.activeJSON.val = true;

        context.motionAnimationMaster.StopPlayback();
        context.motionAnimationMaster.ResetAnimation();

        MarkForRecord(context);

        SuperController.singleton.SelectModeAnimationRecord();

        SuperController.singleton.StartCoroutine(WaitForRecordComplete(context));
    }

    public static void MarkForRecord(EmbodyContext context)
    {
        foreach (var controller in context.plugin.containingAtom.freeControllers.Where(fc => fc.possessed))
        {
            var mac = controller.GetComponent<MotionAnimationControl>();
            mac.ClearAnimation();
            mac.armedForRecord = true;
        }

        if (context.eyeTarget.enabledJSON.val)
        {
            var controller = context.plugin.containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");
            var mac = controller.GetComponent<MotionAnimationControl>();
            mac.ClearAnimation();
            mac.armedForRecord = true;
        }
    }

    private static IEnumerator WaitForRecordComplete(EmbodyContext context)
    {
        while (!string.IsNullOrEmpty(SuperController.singleton.helpText))
            yield return 0;
        context.motionAnimationMaster.StopPlayback();
        context.motionAnimationMaster.ResetAnimation();
    }
}
