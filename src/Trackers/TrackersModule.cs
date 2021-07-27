using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ITrackersModule : IEmbodyModule
{
    JSONStorableBool useProfileJSON { get; }
    JSONStorableBool restorePoseAfterPossessJSON { get; }
    JSONStorableBool previewTrackerOffsetJSON { get; }
    List<MotionControllerWithCustomPossessPoint> motionControls { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> viveTrackers { get; }
    IEnumerable<MotionControllerWithCustomPossessPoint> headAndHands { get; }
    MotionControllerWithCustomPossessPoint headMotionControl { get; }
    MotionControllerWithCustomPossessPoint leftHandMotionControl { get; }
    MotionControllerWithCustomPossessPoint rightHandMotionControl { get; }
    List<FreeControllerV3WithSnapshot> controllers { get; }
    void BindFingers();
    void ReleaseFingers();
    void ClearPersonalData();
    void RefreshHands();
}

public class TrackersModule : EmbodyModuleBase, ITrackersModule
{
    public const string Label = "Trackers";

    public override string storeId => "Trackers";
    public override string label => Label;

    protected override bool shouldBeSelectedByDefault => true;

    public List<MotionControllerWithCustomPossessPoint> motionControls { get; } = new List<MotionControllerWithCustomPossessPoint>();
    public MotionControllerWithCustomPossessPoint headMotionControl { get; private set; }
    public MotionControllerWithCustomPossessPoint leftHandMotionControl { get; private set; }
    public MotionControllerWithCustomPossessPoint rightHandMotionControl { get; private set; }

    public IEnumerable<MotionControllerWithCustomPossessPoint> viveTrackers => motionControls
        .Where(t => MotionControlNames.IsViveTracker(t.name));

    public IEnumerable<MotionControllerWithCustomPossessPoint> headAndHands => motionControls
        .Where(t => MotionControlNames.IsHeadOrHands(t.name));

    public List<FreeControllerV3WithSnapshot> controllers { get; } = new List<FreeControllerV3WithSnapshot>();
    public JSONStorableBool useProfileJSON { get; } = new JSONStorableBool("ImportDefaultsOnLoad", true);
    public JSONStorableBool restorePoseAfterPossessJSON { get; } = new JSONStorableBool("RestorePoseAfterPossess", true);
    public JSONStorableBool previewTrackerOffsetJSON { get; } = new JSONStorableBool("PreviewTrackerOffset", false);

    private FreeControllerV3WithSnapshot _leftHandController;
    private FreeControllerV3WithSnapshot _rightHandController;

    public override void Awake()
    {
        base.Awake();

        headMotionControl = AddMotionControl(
            MotionControlNames.Head,
            () => context.head,
            VamConstants.HeadControlName
        );
        leftHandMotionControl = AddMotionControl(
            MotionControlNames.LeftHand,
            () => context.LeftHand(leftHandMotionControl?.useLeapPositioning ?? false),
            VamConstants.LeftHandControlName,
            mc => HandsAdjustments.ConfigureHand(mc, false)
        );
        rightHandMotionControl = AddMotionControl(
            MotionControlNames.RightHand,
            () => context.RightHand(rightHandMotionControl?.useLeapPositioning ?? false),
            VamConstants.RightHandControlName,
            mc => HandsAdjustments.ConfigureHand(mc, true)
        );
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}1", () => context.viveTracker1 != null && context.viveTracker1.gameObject.activeInHierarchy ? context.viveTracker1 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}2", () => context.viveTracker2 != null && context.viveTracker2.gameObject.activeInHierarchy ? context.viveTracker2 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}3", () => context.viveTracker3 != null && context.viveTracker3.gameObject.activeInHierarchy ? context.viveTracker3 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}4", () => context.viveTracker4 != null && context.viveTracker4.gameObject.activeInHierarchy ? context.viveTracker4 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}5", () => context.viveTracker5 != null && context.viveTracker5.gameObject.activeInHierarchy ? context.viveTracker5 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}6", () => context.viveTracker6 != null && context.viveTracker6.gameObject.activeInHierarchy ? context.viveTracker6 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}7", () => context.viveTracker7 != null && context.viveTracker7.gameObject.activeInHierarchy ? context.viveTracker7 : null);
        AddMotionControl($"{MotionControlNames.ViveTrackerPrefix}8", () => context.viveTracker8 != null && context.viveTracker8.gameObject.activeInHierarchy ? context.viveTracker8 : null);

        foreach (var controller in context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith(VamConstants.ControlSuffix)))
        {
            var snapshot = new FreeControllerV3WithSnapshot(controller);
            controllers.Add(snapshot);
            if (controller.name == VamConstants.LeftHandControlName) _leftHandController = snapshot;
            if (controller.name == VamConstants.RightHandControlName) _rightHandController = snapshot;
        }

        previewTrackerOffsetJSON.setCallbackFunction = val =>
        {
            foreach (var mc in motionControls)
            {
                mc.SyncMotionControl();
                if (!val || mc.name != MotionControlNames.Head || mc.currentMotionControl != null && mc.currentMotionControl == context.diagnostics.head)
                {
                    mc.showPreview = val;
                }
            }
        };
    }

    private MotionControllerWithCustomPossessPoint AddMotionControl(string motionControlName, Func<Transform> getMotionControl, string mappedControllerName = null, Action<MotionControllerWithCustomPossessPoint> configure = null)
    {
        var motionControl = MotionControllerWithCustomPossessPoint.Create(motionControlName, getMotionControl, configure);
        motionControl.mappedControllerName = mappedControllerName;
        motionControls.Add(motionControl);
        motionControl.SyncMotionControl();
        return motionControl;
    }

    private void AdjustHeadToEyesOffset()
    {
        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        var eyesCenter = (eyes.First(eye => eye.name == VamConstants.LeftEyeBoneName).transform.localPosition + eyes.First(eye => eye.name == VamConstants.RightEyeBoneName).transform.localPosition) / 2f;
        headMotionControl.offsetControllerBase = -eyesCenter;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        TryBindTrackers();
    }

    public void RefreshHands()
    {
        if (!enabled) return;

        ReleaseFingers();
        foreach (var c in controllers.Where(c => c.handControl != null))
            Release(c);

        Bind(leftHandMotionControl);
        Bind(rightHandMotionControl);
        BindFingers();
    }

    public void TryBindTrackers()
    {
        if(Bind(headMotionControl))
            StartCoroutine(KeepHeadInPlaceCo(NavigationRigSnapshot.Snap()));
        Bind(leftHandMotionControl);
        Bind(rightHandMotionControl);
        BindFingers();
        foreach (var motionControl in viveTrackers)
            Bind(motionControl);
    }

    private static IEnumerator KeepHeadInPlaceCo(NavigationRigSnapshot rigSnapshot)
    {
        // Once bound, prevent vam from moving the head elsewhere due to world scale change
        yield return new WaitForEndOfFrame();
        rigSnapshot.Restore();
        yield return 0f;
        rigSnapshot.Restore();
    }

    private bool Bind(MotionControllerWithCustomPossessPoint motionControl)
    {
        var controllerWithSnapshot = FindController(motionControl);
        if (controllerWithSnapshot == null)
            return false;
        if (controllerWithSnapshot.active)
            return false;

        var controller = controllerWithSnapshot.controller;

        if (motionControl.name == MotionControlNames.Head)
        {
            if (motionControl.mappedControllerName == VamConstants.HeadControlName)
            {
                // Reduce the stretch between the head and the eye by matching the controller and the head bone
                controller.control.position = context.bones.First(b => b.name == VamConstants.HeadBoneName).transform.position;
                AdjustHeadToEyesOffset();
            }

            if (motionControl.currentMotionControl == SuperController.singleton.centerCameraTarget.transform)
            {
                UserPreferences.singleton.headCollider.gameObject.SetActive(false);
                SuperController.singleton.AlignRigAndController(controller, motionControl);
            }
            else
            {
                SuperController.singleton.AlignTransformAndController(controller, motionControl);
            }
        }
        else
        {
            if (motionControl.controlRotation)
                controller.control.rotation = motionControl.controllerPointTransform.rotation;
            if (motionControl.controlPosition)
                controller.control.position = motionControl.controllerPointTransform.position;
        }

        controllerWithSnapshot.active = true;
        Possess(motionControl, controllerWithSnapshot.controller);

        return true;
    }

    public void BindFingers()
    {
        if (leftHandMotionControl.enabled && leftHandMotionControl.fingersTracking && _leftHandController.handControl != null && leftHandMotionControl.mappedControllerName == VamConstants.LeftHandControlName)
        {
            if (leftHandMotionControl.fingersTracking)
                _leftHandController.handControl.allowPossessFingerControlJSON.val = true;
            _leftHandController.handControl.possessed = true;
        }
        if (rightHandMotionControl.enabled && rightHandMotionControl.fingersTracking && _rightHandController.handControl != null && rightHandMotionControl.mappedControllerName == VamConstants.RightHandControlName)
        {
            if (rightHandMotionControl.fingersTracking)
                _rightHandController.handControl.allowPossessFingerControlJSON.val = true;
            _rightHandController.handControl.possessed = true;
        }
    }

    public void ReleaseFingers()
    {
        if (_leftHandController.handControl != null)
            _leftHandController.handControl.possessed = false;
        if (_rightHandController.handControl != null)
            _rightHandController.handControl.possessed = false;
    }

    private FreeControllerV3WithSnapshot FindController(MotionControllerWithCustomPossessPoint motionControl)
    {
        if (!motionControl.enabled) return null;
        if (motionControl.mappedControllerName == null) return null;
        if (context.passenger.selectedJSON.val && motionControl.name == MotionControlNames.Head) return null;
        if (context.snug.selectedJSON.val && (motionControl.name == MotionControlNames.LeftHand || motionControl.name == MotionControlNames.RightHand))
        {
            if (motionControl.name == MotionControlNames.LeftHand && motionControl.mappedControllerName != VamConstants.LeftHandControlName)
                SuperController.LogError($"Embody: Left hand control is mapped to {motionControl.mappedControllerName} but since Snug is activated, your setting will be ignored");
            if (motionControl.name == MotionControlNames.RightHand && motionControl.mappedControllerName != VamConstants.RightHandControlName)
                SuperController.LogError($"Embody: Left hand control is mapped to {motionControl.mappedControllerName} but since Snug is activated, your setting will be ignored");
            return null;
        }
        var controllerWithSnapshot = controllers.FirstOrDefault(cs => cs.controller.name == motionControl.mappedControllerName);
        if (controllerWithSnapshot == null) return null;
        var controller = controllerWithSnapshot.controller;
        if (controller.possessed) return null;
        if (!motionControl.SyncMotionControl()) return null;
        return controllerWithSnapshot;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        ReleaseFingers();

        foreach (var c in controllers)
        {
            Release(c);
        }

        UserPreferences.singleton.headCollider.gameObject.SetActive(UserPreferences.singleton.useHeadCollider);
    }

    private static void Release(FreeControllerV3WithSnapshot c)
    {
        if (!c.active) return;

        c.controller.RestorePreLinkState();
        c.controller.possessed = false;
        c.controller.startedPossess = false;

        var mac = c.controller.GetComponent<MotionAnimationControl>();
        if (mac != null)
        {
            mac.suspendPositionPlayback = false;
            mac.suspendRotationPlayback = false;
        }

        c.active = false;
    }

    public void OnDestroy()
    {
        foreach (var c in motionControls)
        {
            if (c.trackerPointTransform != null)
                Destroy(c.trackerPointTransform.gameObject);
            if (c.controllerPointTransform != null)
                Destroy(c.controllerPointTransform.gameObject);
        }
    }

    private static void Possess(MotionControllerWithCustomPossessPoint motionControl, FreeControllerV3 controller)
    {
        if (!motionControl.controlPosition && !motionControl.controlRotation) return;

        var sc = SuperController.singleton;

        controller.possessed = true;

        var mac = controller.GetComponent<MotionAnimationControl>();
        if (mac != null)
        {
            if (motionControl.controlPosition)
                mac.suspendPositionPlayback = true;
            if (motionControl.controlRotation)
                mac.suspendRotationPlayback = true;
        }

        if (!motionControl.keepCurrentPhysicsHoldStrength)
        {
            if (motionControl.controlPosition)
                controller.RBHoldPositionSpring = sc.possessPositionSpring;
            if (motionControl.controlRotation)
                controller.RBHoldRotationSpring = sc.possessRotationSpring;
        }

        var motionControllerRB = motionControl.controllerPointRB;
        FreeControllerV3.SelectLinkState linkState;
        if (!motionControl.controlRotation)
            linkState = FreeControllerV3.SelectLinkState.Position;
        else if (!motionControl.controlPosition)
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        else
            linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
        controller.SelectLinkToRigidbody(
            motionControllerRB,
            linkState
        );

        controller.canGrabPosition = false;
        controller.canGrabRotation = false;
    }

    public void ClearPersonalData()
    {
        foreach (var mc in context.trackers.motionControls)
            mc.ResetToDefault(true);
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        if (toScene)
        {
            useProfileJSON.StoreJSON(jc);
        }

        var doExportProfile = toScene && !useProfileJSON.val || toProfile;
        if (doExportProfile)
        {
            restorePoseAfterPossessJSON.StoreJSON(jc);
        }

        var motionControlsJSON = new JSONClass();
        foreach (var motionControl in motionControls)
            motionControlsJSON[motionControl.name] = motionControl.GetJSON(doExportProfile);
        jc["MotionControls"] = motionControlsJSON;
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromScene)
        {
            useProfileJSON.RestoreFromJSON(jc);
        }

        var doImportProfile = fromScene && !useProfileJSON.val || fromProfile;
        if (doImportProfile)
        {
            restorePoseAfterPossessJSON.RestoreFromJSON(jc);
        }
        var motionControlsJSON = jc["MotionControls"].AsObject;
        foreach (var motionControlName in motionControlsJSON.Keys)
        {
            var motionControlJSON = motionControlsJSON[motionControlName];
            var motionControl = motionControls.FirstOrDefault(fc => fc.name == motionControlName);
            motionControl?.RestoreFromJSON(motionControlJSON, doImportProfile);
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        useProfileJSON.SetValToDefault();
        restorePoseAfterPossessJSON.SetValToDefault();
        previewTrackerOffsetJSON.SetValToDefault();

        foreach (var mc in context.trackers.motionControls)
            mc.ResetToDefault();
    }
}
