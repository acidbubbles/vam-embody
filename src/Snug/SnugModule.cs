using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ISnugModule : IEmbodyModule
{
    List<ControllerAnchorPoint> anchorPoints { get; }
    JSONStorableFloat falloffDistanceJSON { get; }
    JSONStorableFloat falloffMidPointJSON { get; }
    JSONStorableBool previewSnugOffsetJSON { get; }
    JSONStorableBool disableSelfGrabJSON { get; }
    SnugAutoSetup autoSetup { get; }
    void ClearPersonalData();
}

public class SnugModule : EmbodyModuleBase, ISnugModule
{
    public const string Label = "Snug";

    public override string storeId => "Snug";
    public override string label => Label;

    public SnugAutoSetup autoSetup { get; private set; }

    public JSONStorableBool previewSnugOffsetJSON { get; private set; }
    public JSONStorableBool disableSelfGrabJSON { get; set; }
    public JSONStorableFloat falloffDistanceJSON { get; private set; }
    public JSONStorableFloat falloffMidPointJSON { get; private set; }
    private SnugHand _lHand;
    private SnugHand _rHand;
    public List<ControllerAnchorPoint> anchorPoints { get; } = new List<ControllerAnchorPoint>();

    private readonly List<FreeControllerV3Snapshot> _previousState = new List<FreeControllerV3Snapshot>();

    public override void Awake()
    {
        try
        {
            base.Awake();

            autoSetup = new SnugAutoSetup(context.containingAtom, this);

            disableSelfGrabJSON = new JSONStorableBool("DisablePersonGrab", true);
            falloffDistanceJSON = new JSONStorableFloat("Falloff", 0.15f, 0f, 5f, false);
            falloffMidPointJSON = new JSONStorableFloat("Falloff", 0.3f, 0.01f, 0.99f, true);

            _lHand = new SnugHand { controller = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "lHandControl") };
            _rHand = new SnugHand { controller = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "rHandControl") };

            InitAnchors();
            InitVisualCues();
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(Awake)}: {exc}");
        }
    }

    private void InitVisualCues()
    {
        previewSnugOffsetJSON = new JSONStorableBool("PreviewSnugOffset", false, val =>
        {
            if (val)
                CreateVisualCues();
            else
                DestroyVisualCues();
        })
        {
            isStorable = false
        };
    }

    private void InitAnchors()
    {
        var bones = containingAtom.GetComponentsInChildren<DAZBone>();

        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Crown",
            label = "Crown",
            bone = bones.First(rb => rb.name == "head").transform,
            inGameOffsetDefault = new Vector3(0, 0.18f, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
            active = true,
            locked = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Head",
            label = "Head",
            bone = bones.First(rb => rb.name == "head").transform,
            inGameOffsetDefault = new Vector3(0, -0.01f, 0.02f),
            inGameSizeDefault = new Vector3(0.15f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.18f, 0, 0.24f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Chest",
            label = "Chest",
            bone = bones.First(rb => rb.name == "chest").transform,
            inGameOffsetDefault = new Vector3(0, 0.05f, 0.046f),
            inGameSizeDefault = new Vector3(0.28f, 0, 0.26f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.3f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Abdomen",
            label = "Abdomen",
            bone = bones.First(rb => rb.name == "abdomen").transform,
            inGameOffsetDefault = new Vector3(0, 0.078f, 0.042f),
            inGameSizeDefault = new Vector3(0.24f, 0, 0.18f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.26f, 0, 0.28f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Hips",
            label = "Hips",
            bone = bones.First(rb => rb.name == "hip").transform,
            inGameOffsetDefault = new Vector3(0, -0.088f, -0.009f),
            inGameSizeDefault = new Vector3(0.36f, 0, 0.24f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.32f, 0, 0.3f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Thighs",
            label = "Thighs",
            // TODO: We could try and average this instead, OR define an absolute value
            bone = bones.First(rb => rb.name == "pelvis").transform,
            inGameOffsetDefault = new Vector3(0, -0.35f, 0),
            inGameSizeDefault = new Vector3(0.38f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.38f, 0, 0.2f),
            auto = false,
            active = true,
            locked = false
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Feet",
            label = "Feet",
            // TODO: We could try and average this instead, OR define an absolute value
            bone = bones.First(rb => rb.name == "lFoot").transform,
            inGameOffsetDefault = new Vector3(0, 0, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
            auto = false,
            active = true,
            locked = true
        });

        foreach (var anchorPoint in anchorPoints)
        {
            anchorPoint.InitFromDefault();
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

    #region Lifecycle

    public override bool BeforeEnable()
    {
        if (!context.trackers.selectedJSON.val)
        {
            SuperController.LogError("Embody: Snug requires the Trackers module.");
            return false;
        }

        autoSetup.AutoSetup();

        return true;
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
                if (!context.trackers.motionControls.Any(mc => mc.mappedControllerName == fc.name && mc.enabled))
                    _previousState.Add(FreeControllerV3Snapshot.Snap(fc));
                fc.canGrabPosition = false;
                fc.canGrabRotation = false;
            }
        }

        try
        {
            EnableHand(_lHand, MotionControlNames.LeftHand);
            EnableHand(_rHand, MotionControlNames.RightHand);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to initialize Snug. {exc}");
            enabledJSON.val = false;
        }

        if (!_lHand.active || !_rHand.active)
        {
            SuperController.LogError($"Embody: Failed to initialize Snug hands. No motion tracker to bind to.");
            enabledJSON.val = false;
        }
    }

    private void EnableHand(SnugHand hand, string motionControlName)
    {
        var motionControl = context.trackers.motionControls.FirstOrDefault(mc => mc.name == motionControlName);
        if (motionControl == null) return;
        if (!motionControl.enabled) return;
        if (!motionControl.SyncMotionControl()) return;
        hand.motionControl = motionControl;
        hand.snapshot = FreeControllerV3Snapshot.Snap(hand.controller);
        hand.active = true;
        hand.controller.canGrabPosition = false;
        hand.controller.canGrabRotation = false;
        hand.controller.currentPositionState = FreeControllerV3.PositionState.On;
        hand.controller.currentRotationState = FreeControllerV3.RotationState.On;
        hand.controller.possessed = true;
        if (context.trackers.enableHandsGraspJSON.val)
            (hand.controller.GetComponent<HandControl>() ?? hand.controller.GetComponent<HandControlLink>().handControl).possessed = true;
        if (previewSnugOffsetJSON.val) hand.showCueLine = true;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        DisableHand(_lHand);
        DisableHand(_rHand);

        foreach (var c in _previousState)
        {
            c.controller.canGrabPosition = c.canGrabPosition;
            c.controller.canGrabRotation = c.canGrabRotation;
        }
        _previousState.Clear();
    }

    private void DisableHand(SnugHand hand)
    {
        if (!hand.active) return;
        hand.controller.possessed = false;
        (hand.controller.GetComponent<HandControl>() ?? hand.controller.GetComponent<HandControlLink>().handControl).possessed = false;
        hand.active = false;
        if (hand.snapshot != null)
        {
            hand.snapshot.Restore(context.trackers.restorePoseAfterPossessJSON.val);
            hand.snapshot = null;
        }
        hand.showCueLine = false;
    }

    public void OnDestroy()
    {
        OnDisable();
        DestroyVisualCues();
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

        var possessPointPosition = hand.motionControl.possessPointTransform.position;
        var finalPositionToControlPoint = finalPosition + (possessPointPosition - motionControlPosition);

        if (previewSnugOffsetJSON.val)
        {
            visualCueLinePoints[0] = motionControlPosition;
            visualCueLinePoints[1] = finalPosition;
        }

        hand.controllerRigidbody.MovePosition(Vector3.Lerp(possessPointPosition, finalPositionToControlPoint, effectWeight));
        hand.controllerRigidbody.MoveRotation(motionControl.possessPointTransform.rotation);
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

    private static Vector3 ComputeHandPositionFromAnchor(Vector3 upperPosition, Vector3 motionControlPosition, ControllerAnchorPoint upper)
    {
        var anchorPosition = upperPosition;
        var realLifeSize = upper.realLifeSize;

        var realOffset = upper.realLifeOffset;
        var inGameSize = upper.inGameSize;
        var scale = new Vector3(inGameSize.x / realLifeSize.x, 1f, inGameSize.z / realLifeSize.z);
        var actualRelativePosition = motionControlPosition - anchorPosition;
        var scaled = Vector3.Scale(actualRelativePosition, scale);
        var finalPosition = anchorPosition + scaled - realOffset;

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
        _lHand.showCueLine = false;
        _rHand.showCueLine = false;

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

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

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

        falloffDistanceJSON.StoreJSON(jc);
        disableSelfGrabJSON.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

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
                anchor.active = anchorJSON["Active"]?.Value != "false";
                anchor.Update();
            }
        }

        falloffDistanceJSON.RestoreFromJSON(jc);
        disableSelfGrabJSON.RestoreFromJSON(jc);
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        foreach (var anchor in anchorPoints)
        {
            anchor.InitFromDefault();
            anchor.Update();
        }

        falloffDistanceJSON.SetValToDefault();
        disableSelfGrabJSON.SetValToDefault();
    }

    #endregion
}
