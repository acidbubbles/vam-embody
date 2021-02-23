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
    public IDiagnosticsModule diagnostics;
    public IWizard wizard;
    public EmbodyScaleChangeReceiver scaleChangeReceiver;

    // ReSharper disable once Unity.NoNullCoalescing
    public Transform head => diagnostics.head ?? SuperController.singleton.centerCameraTarget.transform;

    public Transform leftHand
    {
        get
        {
            if (!ReferenceEquals(diagnostics.leftHand, null))
                return diagnostics.leftHand;

            if (SuperController.singleton.isOVR && SuperController.singleton.ovrHandInputLeft.enabled)
                return SuperController.singleton.touchObjectLeft;
            if (SuperController.singleton.isOpenVR && SuperController.singleton.steamVRHandInputLeft.enabled)
                return SuperController.singleton.viveObjectLeft;
            if (SuperController.singleton.leapHandModelControl.leftHandEnabled)
                return SuperController.singleton.leapHandLeft;
            return null;
        }
    }

    public Transform rightHand
    {
        get
        {
            if (!ReferenceEquals(diagnostics.rightHand, null))
                return diagnostics.rightHand;

            if (SuperController.singleton.isOVR && SuperController.singleton.ovrHandInputRight.enabled)
                return SuperController.singleton.touchObjectRight;
            if (SuperController.singleton.isOpenVR && SuperController.singleton.steamVRHandInputRight.enabled)
                return SuperController.singleton.viveObjectRight;
            if (SuperController.singleton.leapHandModelControl.rightHandEnabled)
                return SuperController.singleton.leapHandRight;
            return null;
        }
    }

    // ReSharper disable Unity.NoNullCoalescing
    public Transform viveTracker1 => diagnostics.viveTracker1 ?? SuperController.singleton.viveTracker1;
    public Transform viveTracker2 => diagnostics.viveTracker2 ?? SuperController.singleton.viveTracker2;
    public Transform viveTracker3 => diagnostics.viveTracker3 ?? SuperController.singleton.viveTracker3;
    public Transform viveTracker4 => diagnostics.viveTracker4 ?? SuperController.singleton.viveTracker4;
    public Transform viveTracker5 => diagnostics.viveTracker5 ?? SuperController.singleton.viveTracker5;
    public Transform viveTracker6 => diagnostics.viveTracker6 ?? SuperController.singleton.viveTracker6;
    public Transform viveTracker7 => diagnostics.viveTracker7 ?? SuperController.singleton.viveTracker7;
    public Transform viveTracker8 => diagnostics.viveTracker8 ?? SuperController.singleton.viveTracker8;
    // ReSharper restore Unity.NoNullCoalescing

    public DAZBone[] bones { get; private set; }

    public EmbodyContext(MVRScript plugin, IEmbody embody)
    {
        this.plugin = plugin;
        this.embody = embody;
        containingAtom = plugin.containingAtom;
        bones = containingAtom.GetComponentsInChildren<DAZBone>();
    }

    public void Refresh()
    {
        if (!embody.activeJSON.val) return;
        embody.activeJSON.val = false;
        embody.activeJSON.val = true;
    }
}
