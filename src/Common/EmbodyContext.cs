using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class EmbodyContext
{
    public readonly MVRScript plugin;
    public readonly IEmbody embody;

    public Atom containingAtom { get; }

    public IAutomationModule automation;
    public IWorldScaleModule worldScale;
    public IHideGeometryModule hideGeometry;
    public IOffsetCameraModule offsetCamera;
    public IPassengerModule passenger;
    public ITrackersModule trackers;
    public ISnugModule snug;
    public IEyeTargetModule eyeTarget;
    public DiagnosticsModule diagnostics;
    public IWizard wizard;
    public EmbodyScaleChangeReceiver scaleChangeReceiver;

    public Transform head { get; private set; }
    public Transform leftHand { get; private set; }
    public Transform rightHand { get; private set; }
    public Transform viveTracker1 { get; private set; }
    public Transform viveTracker2 { get; private set; }
    public Transform viveTracker3 { get; private set; }
    public Transform viveTracker4 { get; private set; }
    public Transform viveTracker5 { get; private set; }
    public Transform viveTracker6 { get; private set; }
    public Transform viveTracker7 { get; private set; }
    public Transform viveTracker8 { get; private set; }
    public DAZBone[] bones { get; private set; }

    public EmbodyContext(MVRScript plugin, IEmbody embody)
    {
        this.containingAtom = plugin.containingAtom;
        this.plugin = plugin;
        this.embody = embody;
    }

    [SuppressMessage("ReSharper", "Unity.NoNullPropagation")]
    [SuppressMessage("ReSharper", "Unity.NoNullCoalescing")]
    public void Initialize()
    {
        var sc = SuperController.singleton;
        const string prefix = "EMBODY_DEBUG#";
        head = GetDebugAtom($"{prefix}head") ?? sc.centerCameraTarget.transform;
        leftHand = GetDebugAtom($"{prefix}lHand") ?? GetHand(sc.touchObjectLeft, sc.viveObjectLeft);
        rightHand = GetDebugAtom($"{prefix}rHand") ?? GetHand(sc.touchObjectRight, sc.viveObjectRight);
        viveTracker1 = GetDebugAtom($"{prefix}viveTracker1") ?? sc.viveTracker1;
        viveTracker2 = GetDebugAtom($"{prefix}viveTracker2") ?? sc.viveTracker2;
        viveTracker3 = GetDebugAtom($"{prefix}viveTracker3") ?? sc.viveTracker3;
        viveTracker4 = GetDebugAtom($"{prefix}viveTracker4") ?? sc.viveTracker4;
        viveTracker5 = GetDebugAtom($"{prefix}viveTracker5") ?? sc.viveTracker5;
        viveTracker6 = GetDebugAtom($"{prefix}viveTracker6") ?? sc.viveTracker6;
        viveTracker7 = GetDebugAtom($"{prefix}viveTracker7") ?? sc.viveTracker7;
        viveTracker8 = GetDebugAtom($"{prefix}viveTracker8") ?? sc.viveTracker8;
        if (bones == null) bones = containingAtom.GetComponentsInChildren<DAZBone>();
    }

    private static Transform GetDebugAtom(string uid)
    {
        var atom = SuperController.singleton.GetAtomByUid(uid);
        if (atom == null || !atom.on) return null;
        return atom.mainController.control;
    }

    private Transform GetHand(Transform ovrHand, Transform viveHand)
    {
        if (SuperController.singleton.isOVR)
            return ovrHand;
        if (SuperController.singleton.isOpenVR)
            return viveHand;
        return null;
    }

    public void Refresh()
    {
        if (!embody.activeJSON.val) return;
        embody.activeJSON.val = false;
        embody.activeJSON.val = true;
    }
}
