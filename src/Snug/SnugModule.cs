using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ISnugModule : IEmbodyModule
{
    JSONStorableBool useProfileJSON { get; }
    List<ControllerAnchorPoint> anchorPoints { get; }
    JSONStorableFloat falloffDistanceJSON { get; }
    JSONStorableFloat falloffMidPointJSON { get; }
    JSONStorableBool previewSnugOffsetJSON { get; }
    JSONStorableBool disableSelfGrabJSON { get; }
    SnugAutoSetup autoSetup { get; }
    void ClearPersonalData();
    void ScaleChanged();
    void RefreshHands();
}

public class SnugModule : EmbodyModuleBase, ISnugModule
{
    public const string Label = "Snug";

    public override string storeId => "Snug";
    public override string label => Label;

    public SnugAutoSetup autoSetup { get; private set; }

    public JSONStorableBool useProfileJSON { get; } = new JSONStorableBool("ImportDefaultsOnLoad", true);
    public JSONStorableBool previewSnugOffsetJSON { get; } = new JSONStorableBool("PreviewSnugOffset", false);
    public JSONStorableBool disableSelfGrabJSON { get; } = new JSONStorableBool("DisablePersonGrab", true);
    public JSONStorableFloat falloffDistanceJSON { get; } = new JSONStorableFloat("Falloff", 0.15f, 0f, 5f, false);
    public JSONStorableFloat falloffMidPointJSON { get; } = new JSONStorableFloat("Falloff", 0.3f, 0.01f, 0.99f, true);
    public List<ControllerAnchorPoint> anchorPoints { get; } = new List<ControllerAnchorPoint>();

    private SnugHand _lHand;
    private SnugHand _rHand;

    private DAZBone _lToe;
    private DAZBone _rToe;

    public override void InitStorables()
    {
        base.InitStorables();

        InitAnchors();

        previewSnugOffsetJSON.setCallbackFunction = val =>
        {
            if (val)
                CreateVisualCues();
            else
                DestroyVisualCues();
        };
    }

    private void InitAnchors()
    {
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Crown",
            label = "Crown",
            initBone = () => context.bones.First(rb => rb.name == "head").transform,
            inGameOffsetDefault = new Vector3(0, 0.18f, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
            locked = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Head",
            label = "Head",
            initBone = () => context.bones.First(rb => rb.name == "head").transform,
            inGameOffsetDefault = new Vector3(0, -0.01f, 0.02f),
            inGameSizeDefault = new Vector3(0.15f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.18f, 0, 0.24f),
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Chest",
            label = "Chest",
            initBone = () => context.bones.First(rb => rb.name == "chest").transform,
            inGameOffsetDefault = new Vector3(0, 0.05f, 0.046f),
            inGameSizeDefault = new Vector3(0.28f, 0, 0.26f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.3f),
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Abdomen",
            label = "Abdomen",
            initBone = () => context.bones.First(rb => rb.name == "abdomen").transform,
            inGameOffsetDefault = new Vector3(0, 0.078f, 0.042f),
            inGameSizeDefault = new Vector3(0.24f, 0, 0.18f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.26f, 0, 0.28f),
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Hips",
            label = "Hips",
            initBone = () => context.bones.First(rb => rb.name == "hip").transform,
            inGameOffsetDefault = new Vector3(0, -0.088f, -0.009f),
            inGameSizeDefault = new Vector3(0.36f, 0, 0.24f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.32f, 0, 0.3f),
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Thighs",
            label = "Thighs",
            initBone = () => context.bones.First(rb => rb.name == "pelvis").transform,
            inGameOffsetDefault = new Vector3(0, -0.35f, 0),
            inGameSizeDefault = new Vector3(0.38f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.38f, 0, 0.2f),
            auto = false,
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Feet",
            label = "Feet",
            initBone = () => context.bones.First(rb => rb.name == "pelvis").transform,
            inGameOffsetDefault = new Vector3(0, -1.2f, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
            auto = false,
            locked = true,
            floor = true
        });

        foreach (var anchorPoint in anchorPoints)
        {
            anchorPoint.InitFromDefault();
        }
    }

    public override void InitReferences()
    {
        base.InitReferences();

        autoSetup = new SnugAutoSetup(context.containingAtom, this);

        _lHand = new SnugHand { controller = containingAtom.freeControllers.First(fc => fc.name == "lHandControl") };
        _rHand = new SnugHand { controller = containingAtom.freeControllers.First(fc => fc.name == "rHandControl") };

        _lToe = context.bones.First(b => b.name == "lToe");
        _rToe = context.bones.First(b => b.name == "rToe");

        foreach (var anchorPoint in anchorPoints)
        {
            anchorPoint.scaleChangeReceiver = context.scaleChangeReceiver;
            anchorPoint.bone = anchorPoint.initBone();
            anchorPoint.Update();
        }
    }

    public void ClearPersonalData()
    {
        foreach (var anchorPoint in anchorPoints)
        {
            anchorPoint.realLifeSize = anchorPoint.realLifeSizeDefault;
            anchorPoint.realLifeOffset = anchorPoint.realLifeOffsetDefault;
            anchorPoint.Update();
        }
    }

    public void ScaleChanged()
    {
        SuperController.singleton.StartCoroutine(ScaleChangedCo());
    }

    private IEnumerator ScaleChangedCo()
    {
        if (anchorPoints.Count == 0) yield break;
        DestroyVisualCues();
        for (var i = 0; i < 45; i++)
            yield return 0;
        if (this == null) yield break;
        if (previewSnugOffsetJSON.val)
            CreateVisualCues();
    }

    #region Lifecycle

    public override bool Validate()
    {
        if (!context.trackers.selectedJSON.val)
        {
            SuperController.LogError("Embody: Snug requires the Trackers module.");
            return false;
        }

        return true;
    }

    public override void PreActivate()
    {
        autoSetup.AutoSetup();
    }

    public override void OnEnable()
    {
        base.OnEnable();

        if (disableSelfGrabJSON.val)
        {
            foreach (var fc in containingAtom.freeControllers)
            {
                if (fc == _lHand.controller || fc == _rHand.controller) continue;
                if (!fc.canGrabPosition && !fc.canGrabRotation) continue;
                if (!fc.name.EndsWith("Control")) continue;
                if (fc.control == null) continue;
                fc.canGrabPosition = false;
                fc.canGrabRotation = false;
            }
        }

        try
        {
            EnableHand(_lHand, MotionControlNames.LeftHand);
            EnableHand(_rHand, MotionControlNames.RightHand);
            context.trackers.BindFingers();
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to initialize Snug. {exc}");
            enabledJSON.val = false;
        }
    }

    private void EnableHand(SnugHand hand, string motionControlName)
    {
        var motionControl = context.trackers.motionControls.FirstOrDefault(mc => mc.name == motionControlName);
        if (motionControl == null) return;
        if (!motionControl.enabled) return;
        if (!motionControl.SyncMotionControl()) return;
        if (!motionControl.controlPosition) return;
        hand.motionControl = motionControl;
        hand.snapshot = FreeControllerV3Snapshot.Snap(hand.controller);
        hand.active = true;
        hand.controller.canGrabPosition = false;
        hand.controller.canGrabRotation = false;
        hand.controller.currentPositionState = FreeControllerV3.PositionState.On;
        if (motionControl.controlRotation)
            hand.controller.currentRotationState = FreeControllerV3.RotationState.On;
        hand.controller.possessed = true;
        var mac = hand.controller.GetComponent<MotionAnimationControl>();
        if (mac != null)
        {
            mac.suspendPositionPlayback = true;
            if (motionControl.controlRotation)
                mac.suspendRotationPlayback = true;
        }
        if (!motionControl.keepCurrentPhysicsHoldStrength)
        {
            hand.controller.RBHoldPositionSpring = SuperController.singleton.possessPositionSpring;
            if (motionControl.controlRotation)
                hand.controller.RBHoldRotationSpring = SuperController.singleton.possessRotationSpring;
        }
        if (previewSnugOffsetJSON.val) hand.showCueLine = true;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        context.trackers.ReleaseFingers();
        DisableHand(_lHand);
        DisableHand(_rHand);
    }

    private void DisableHand(SnugHand hand)
    {
        if (hand == null) return;
        if (!hand.active) return;
        hand.controller.possessed = false;
        (hand.controller.GetComponent<HandControl>() ?? hand.controller.GetComponent<HandControlLink>().handControl).possessed = false;
        hand.active = false;
        var mac = hand.controller.GetComponent<MotionAnimationControl>();
        if (mac != null)
        {
            mac.suspendPositionPlayback = false;
            mac.suspendRotationPlayback = false;
        }
        if (hand.snapshot != null)
        {
            hand.snapshot.Restore(false);
            hand.snapshot = null;
        }
        hand.showCueLine = false;
    }

    public void OnDestroy()
    {
        OnDisable();
        DestroyVisualCues();
    }

    public void RefreshHands()
    {
        if (!enabled) return;

        context.trackers.ReleaseFingers();
        DisableHand(_lHand);
        DisableHand(_rHand);

        EnableHand(_lHand, MotionControlNames.LeftHand);
        EnableHand(_rHand, MotionControlNames.RightHand);
        context.trackers.BindFingers();
    }

    #endregion

    #region Update

    public void Update()
    {
        try
        {
            _lHand.SyncCueLine();
            _rHand.SyncCueLine();
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(Update)}: {exc}");
            enabled = false;
        }
    }

    public void FixedUpdate()
    {
        try
        {
            ProcessHand(_lHand);
            ProcessHand(_rHand);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(FixedUpdate)}: {exc}");
            enabled = false;
        }
    }

    private void ProcessHand(SnugHand hand)
    {
        if (!hand.active) return;

        var motionControl = hand.motionControl;
        var motionControlPosition = motionControl.currentMotionControl.position;
        var visualCueLinePoints = hand.visualCueLinePoints;

        if (!hand.motionControl.currentMotionControl.gameObject.activeInHierarchy) return;

        // Find the anchor over and under the controller
        ControllerAnchorPoint lower = null;
        ControllerAnchorPoint upper = null;
        for (var i = anchorPoints.Count - 1; i >= 0; i--)
        {
            var anchorPoint = anchorPoints[i];
            if (!anchorPoint.active) continue;
            var anchorPointPos = anchorPoint.GetAdjustedWorldPosition();
            if (motionControlPosition.y > anchorPointPos.y && (lower == null || anchorPointPos.y > lower.GetAdjustedWorldPosition().y))
            {
                lower = anchorPoint;
            }
            else if (motionControlPosition.y < anchorPointPos.y && (upper == null || anchorPointPos.y < upper.GetAdjustedWorldPosition().y))
            {
                upper = anchorPoint;
            }
        }

        if (lower == null)
            lower = upper;
        else if (upper == null)
            upper = lower;

        // Find the weight of both anchors (closest = strongest effect)
        // ReSharper disable once PossibleNullReferenceException
        var upperPosition = upper.GetAdjustedWorldPosition();
        // ReSharper disable once PossibleNullReferenceException
        var lowerPosition = lower.GetAdjustedWorldPosition();
        if (lower.floor) lowerPosition = new Vector3(motionControlPosition.x, Mathf.Min(_lToe.transform.position.y, _rToe.transform.position.y), motionControlPosition.z);
        var yUpperDelta = upperPosition.y - motionControlPosition.y;
        var yLowerDelta = motionControlPosition.y - lowerPosition.y;
        var totalDelta = yLowerDelta + yUpperDelta;
        var upperWeight = totalDelta == 0 ? 1f : yLowerDelta / totalDelta;
        var lowerWeight = 1f - upperWeight;

        var finalPositionUpper =  ComputeHandPositionFromAnchor(upperPosition, motionControlPosition, upper);
        var finalPositionLower =  ComputeHandPositionFromAnchor(lowerPosition, motionControlPosition, lower);
        var finalPosition = new Vector3(
            Mathf.SmoothStep(finalPositionUpper.x, finalPositionLower.x, lowerWeight),
            Mathf.SmoothStep(finalPositionUpper.y, finalPositionLower.y, lowerWeight),
            Mathf.SmoothStep(finalPositionUpper.z, finalPositionLower.z, lowerWeight)
        );

        var effectWeight = ComputeEffectWeight(upper, lower, lowerWeight, motionControlPosition);

        var possessPointPosition = hand.motionControl.controllerPointTransform.position;
        var finalPositionToControlPoint = finalPosition + (possessPointPosition - motionControlPosition);

        if (previewSnugOffsetJSON.val)
        {
            visualCueLinePoints[0] = motionControlPosition;
            visualCueLinePoints[1] = Vector3.Lerp(motionControlPosition, finalPosition, effectWeight);
        }

        hand.controllerRigidbody.MovePosition(Vector3.Lerp(possessPointPosition, finalPositionToControlPoint, effectWeight));
        hand.controllerRigidbody.MoveRotation(motionControl.controllerPointTransform.rotation);
    }

    private float ComputeEffectWeight(ControllerAnchorPoint upper, ControllerAnchorPoint lower, float lowerWeight, Vector3 motionControlPosition)
    {
        if (falloffDistanceJSON.val == 0)
            return 1f;
        var realLifeSize = Vector3.Lerp(upper.realLifeSize, lower.realLifeSize, lowerWeight);
        var anchorPosition = Vector3.Lerp(upper.GetAdjustedWorldPosition(), lower.GetAdjustedWorldPosition(), lowerWeight);
        var anchorRotation = Quaternion.Slerp(upper.bone.transform.rotation, lower.bone.transform.rotation, lowerWeight);

        var angle = Mathf.Deg2Rad * Vector3.SignedAngle(anchorRotation * Vector3.forward, motionControlPosition - anchorPosition, anchorRotation * Vector3.up);
        var anchorHook = anchorPosition + new Vector3(Mathf.Sin(angle) * realLifeSize.x / 2f, 0f, Mathf.Cos(angle) * realLifeSize.z / 2f);
        var hookDistanceFromAnchor = Vector3.Distance(anchorPosition, anchorHook);
        var distanceFromAnchorHook = Mathf.Clamp(Vector3.Distance(anchorPosition, motionControlPosition) - hookDistanceFromAnchor, 0f, falloffDistanceJSON.val);
        var effectWeight = 1f - (distanceFromAnchorHook / falloffDistanceJSON.val);
        var exponentialWeight = ExponentialScale(effectWeight, falloffMidPointJSON.val);
        return exponentialWeight;
    }

    private static float ExponentialScale(float inputValue, float midValue)
    {
        var m = 1f / midValue;
        var c = Mathf.Log(Mathf.Pow(m - 1, 2));
        var b = 1f / (Mathf.Exp(c) - 1);
        var a = -1 * b;
        return a + b * Mathf.Exp(c * inputValue);
    }

    private static Vector3 ComputeHandPositionFromAnchor(Vector3 anchorPosition, Vector3 motionControlPosition, ControllerAnchorPoint anchorPoint)
    {
        var realLifeSize = anchorPoint.realLifeSize;
        var realOffset = anchorPoint.realLifeOffset;
        var inGameSize = anchorPoint.inGameSize;
        var scale = new Vector3(inGameSize.x / realLifeSize.x, 1f, inGameSize.z / realLifeSize.z);
        var actualRelativePosition = motionControlPosition - anchorPosition;
        var scaled = Vector3.Scale(actualRelativePosition, scale);
        var finalPosition = anchorPosition + scaled - (realOffset * SuperController.singleton.worldScale);

        return finalPosition;
    }

    #endregion

    #region Debuggers

    private void CreateVisualCues()
    {
        DestroyVisualCues();

        autoSetup.AutoSetup();

        _lHand.showCueLine = true;
        _rHand.showCueLine = true;

        foreach (var anchorPoint in anchorPoints)
        {
            if (anchorPoint.locked) continue;

            anchorPoint.inGameCue = new ControllerAnchorPointVisualCue(anchorPoint.bone.transform, new Color(0.8f, 0.6f, 0.6f));
            anchorPoint.Update();

            anchorPoint.realLifeCue = new ControllerAnchorPointVisualCue(anchorPoint.bone.transform, new Color(0.4f, 0.8f, 0.4f));
            anchorPoint.Update();
        }
    }

    private void DestroyVisualCues()
    {
        if (_lHand != null) _lHand.showCueLine = false;
        if (_rHand != null) _rHand.showCueLine = false;

        foreach (var anchor in anchorPoints)
        {
            Destroy(anchor.inGameCue?.gameObject);
            anchor.inGameCue = null;
            Destroy(anchor.realLifeCue?.gameObject);
            anchor.realLifeCue = null;
        }
    }

    #endregion

    #region Load / Save

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        if (toScene)
        {
            useProfileJSON.StoreJSON(jc);
        }

        if (toScene && !useProfileJSON.val || toProfile)
        {
            falloffDistanceJSON.StoreJSON(jc);
            disableSelfGrabJSON.StoreJSON(jc);

            var anchors = new JSONClass();
            foreach (var anchor in anchorPoints)
            {
                anchors[anchor.id] = new JSONClass
                {
                    /*
                    {"InGameOffset", anchor.InGameOffset.ToJSON()},
                    {"InGameSize", anchor.InGameSize.ToJSON()},
                    */
                    {"RealLifeOffset", anchor.realLifeOffset.ToJSON()},
                    {"RealLifeScale", anchor.realLifeSize.ToJSON()},
                    {"Active", anchor.active ? "true" : "false"},
                };
            }

            jc["Anchors"] = anchors;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromScene)
        {
            useProfileJSON.RestoreFromJSON(jc);
        }

        if (fromScene && !useProfileJSON.val || fromProfile)
        {
            disableSelfGrabJSON.RestoreFromJSON(jc);
            falloffDistanceJSON.RestoreFromJSON(jc);
            var anchorsJSON = jc["Anchors"];
            if (anchorsJSON != null)
            {
                foreach (var anchor in anchorPoints)
                {
                    var anchorJSON = anchorsJSON[anchor.id];
                    if (anchorJSON == null) continue;
                    /*
                    anchor.InGameOffset = anchorJSON["InGameOffset"].ToVector3(anchor.InGameOffset);
                    anchor.InGameSize = anchorJSON["InGameSize"].ToVector3(anchor.InGameSize);
                    */
                    anchor.realLifeOffset = anchorJSON["RealLifeOffset"].ToVector3(anchor.realLifeOffset);
                    anchor.realLifeSize = anchorJSON["RealLifeScale"].ToVector3(anchor.realLifeSize);
                    if (!fromProfile)
                        anchor.active = anchorJSON["Active"]?.Value != "false";
                    anchor.Update();
                }
            }
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        useProfileJSON.SetValToDefault();
        disableSelfGrabJSON.SetValToDefault();
        falloffDistanceJSON.SetValToDefault();

        foreach (var anchor in anchorPoints)
        {
            anchor.InitFromDefault();
            anchor.Update();
        }
    }

    #endregion
}
