using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class EmbodyContext
{
    public readonly MVRScript plugin;
    public readonly IEmbody embody;
    public Atom containingAtom => plugin.containingAtom;

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
    public Transform viveTracker8 { get; set; }

    public EmbodyContext(MVRScript plugin, IEmbody embody)
    {
        this.plugin = plugin;
        this.embody = embody;

    }

    [SuppressMessage("ReSharper", "Unity.NoNullPropagation")]
    [SuppressMessage("ReSharper", "Unity.NoNullCoalescing")]
    public void Initialize()
    {
        var sc = SuperController.singleton;
        const string prefix = "EMBODY_DEBUG#";
        head = sc.GetAtomByUid($"{prefix}head")?.mainController?.transform ?? sc.centerCameraTarget.transform;
        leftHand = sc.GetAtomByUid($"{prefix}lHand")?.mainController?.transform ?? sc.leftHand;
        rightHand = sc.GetAtomByUid($"{prefix}rHand")?.mainController?.transform ?? sc.rightHand;
        viveTracker1 = sc.viveTracker1;
        viveTracker2 = sc.viveTracker2;
        viveTracker3 = sc.viveTracker3;
        viveTracker4 = sc.viveTracker4;
        viveTracker5 = sc.viveTracker5;
        viveTracker6 = sc.viveTracker6;
        viveTracker7 = sc.viveTracker7;
        viveTracker8 = sc.viveTracker8;
    }

}
