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

    // ReSharper disable once Unity.NoNullPropagation Unity.NoNullCoalescing
    public MotionAnimationMaster motionAnimationMaster => containingAtom.subSceneComponent?.motionAnimationMaster ?? SuperController.singleton.motionAnimationMaster;

    // ReSharper disable once Unity.NoNullCoalescing
    public Transform head => diagnostics.head ?? SuperController.singleton.centerCameraTarget.transform;

    public Transform LeftHand(bool useLeap = false)
    {
        if (!ReferenceEquals(diagnostics.leftHand, null))
            return diagnostics.leftHand;
        if (useLeap)
            return SuperController.singleton.leapHandLeft;
        if (SuperController.singleton.isOVR)
            return SuperController.singleton.touchObjectLeft;
        if (SuperController.singleton.isOpenVR)
            return SuperController.singleton.viveObjectLeft;
        return null;
    }

    public Transform RightHand(bool useLeap = false)
    {
        if (!ReferenceEquals(diagnostics.rightHand, null))
            return diagnostics.rightHand;
        if (useLeap)
            return SuperController.singleton.leapHandRight;
        if (SuperController.singleton.isOVR)
            return SuperController.singleton.touchObjectRight;
        if (SuperController.singleton.isOpenVR)
            return SuperController.singleton.viveObjectRight;

        return null;
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

    public JSONStorableBool leftHandToggle { get; set; }
    public JSONStorableBool rightHandToggle { get; set; }

    public DAZBone[] bones { get; private set; }

    public EmbodyContext(MVRScript plugin, IEmbody embody)
    {
        this.plugin = plugin;
        this.embody = embody;
        containingAtom = plugin.containingAtom;

        leftHandToggle = new JSONStorableBool("Left Hand Enabled", true, val =>
        {
            trackers.leftHandMotionControl.enabled = val;
            trackers.RefreshHands();
            embody.Refresh();
        }) { isStorable = false };
        rightHandToggle = new JSONStorableBool("Right Hand Enabled", true, val =>
        {
            trackers.rightHandMotionControl.enabled = val;
            trackers.RefreshHands();
            embody.Refresh();
        }) { isStorable = false };
    }

    public void InitReferences()
    {
        if (containingAtom.type == "Person")
        {
            bones = containingAtom.transform.Find("rescale2").GetComponentsInChildren<DAZBone>();
        }
    }

    public void Refresh()
    {
        embody.Refresh();
    }

    public void RefreshTriggers()
    {
        leftHandToggle.valNoCallback = trackers.leftHandMotionControl.enabled;
        rightHandToggle.valNoCallback = trackers.rightHandMotionControl.enabled;
    }
}
