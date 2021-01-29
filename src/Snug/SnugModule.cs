using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ISnugModule : IEmbodyModule
{
    List<ControllerAnchorPoint> anchorPoints { get; }
    JSONStorableFloat falloffJSON { get; }
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

    public ITrackersModule trackers;

    public SnugAutoSetup autoSetup { get; private set; }

    public JSONStorableBool previewSnugOffsetJSON { get; private set; }
    public JSONStorableBool disableSelfGrabJSON { get; set; }
    public JSONStorableFloat falloffJSON { get; private set; }
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

            disableSelfGrabJSON = new JSONStorableBool("DisablePersonGrab", false);
            falloffJSON = new JSONStorableFloat("Falloff", 0.3f, 0f, 5f, false);

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
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Crown",
            label = "Crown",
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "head"),
            inGameOffsetDefault = new Vector3(0, 0.2f, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
            active = true,
            locked = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Lips",
            label = "Lips",
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
            inGameOffsetDefault = new Vector3(0, 0, -0.07113313f),
            inGameSizeDefault = new Vector3(0.15f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.18f, 0, 0.24f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Chest",
            label = "Chest",
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
            inGameOffsetDefault = new Vector3(0, 0.0682705f, 0.04585214f),
            inGameSizeDefault = new Vector3(0.28f, 0, 0.26f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.3f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Abdomen",
            label = "Abdomen",
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
            inGameOffsetDefault = new Vector3(0, 0.0770329f, 0.04218798f),
            inGameSizeDefault = new Vector3(0.24f, 0, 0.18f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.26f, 0, 0.28f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Hips",
            label = "Hips",
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "hip"),
            inGameOffsetDefault = new Vector3(0, -0.08762675f, -0.009161186f),
            inGameSizeDefault = new Vector3(0.36f, 0, 0.24f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.32f, 0, 0.3f),
            active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            id = "Feet",
            label = "Feet",
            // TODO: We could try and average this instead, OR define an absolute value
            rigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
            inGameOffsetDefault = new Vector3(0, 0, 0),
            inGameSizeDefault = new Vector3(0.2f, 0, 0.2f),
            realLifeOffsetDefault = Vector3.zero,
            realLifeSizeDefault = new Vector3(0.2f, 0, 0.2f),
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
        if (!trackers.selectedJSON.val)
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
                if (!trackers.motionControls.Any(mc => mc.mappedControllerName == fc.name && mc.enabled))
                    _previousState.Add(FreeControllerV3Snapshot.Snap(fc));
                fc.canGrabPosition = false;
                fc.canGrabRotation = false;
            }
        }

        EnableHand(_lHand, MotionControlNames.LeftHand);
        EnableHand(_rHand, MotionControlNames.RightHand);
    }

    private void EnableHand(SnugHand hand, string motionControlName)
    {
        var motionControl = trackers.motionControls.FirstOrDefault(mc => mc.name == motionControlName);
        if (motionControl == null || !motionControl.SyncMotionControl()) return;
        hand.motionControl = motionControl;
        hand.snapshot = FreeControllerV3Snapshot.Snap(hand.controller);
        hand.active = true;
        hand.controller.canGrabPosition = false;
        hand.controller.canGrabRotation = false;
        hand.controller.possessed = true;
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
            c._controller.canGrabPosition = c._canGrabPosition;
            c._controller.canGrabRotation = c._canGrabRotation;
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
            hand.snapshot.Restore(trackers.restorePoseAfterPossessJSON.val);
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
        var motionControl = hand.motionControl;
        var motionControlPosition = motionControl.currentMotionControl.position;
        var visualCueLinePoints = hand.visualCueLinePoints;

        // Find the anchor over and under the controller
        ControllerAnchorPoint lower = null;
        ControllerAnchorPoint upper = null;
        foreach (var anchorPoint in anchorPoints)
        {
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

        // TODO: If an anchor is higher than one that should be higher, ignore it
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
        var lowerRotation = lower.rigidBody.transform.rotation;
        var upperRotation = upper.rigidBody.transform.rotation;
        // TODO: We can use a bezier curve or similar to make a curve following the angle of the upper/lower controls
        var anchorPosition = Vector3.Lerp(upperPosition, lowerPosition, lowerWeight);
        var anchorRotation = Quaternion.Lerp(upperRotation, lowerRotation, lowerWeight);

        var angle = Mathf.Deg2Rad * Vector3.SignedAngle(anchorRotation * Vector3.forward, motionControlPosition - anchorPosition, anchorRotation * Vector3.up);
        var realLifeSize = Vector3.Lerp(upper.realLifeSize, lower.realLifeSize, lowerWeight);
        var anchorHook = anchorPosition + new Vector3(Mathf.Sin(angle) * realLifeSize.x / 2f, 0f, Mathf.Cos(angle) * realLifeSize.z / 2f);

        var hookDistanceFromAnchor = Vector3.Distance(anchorPosition, anchorHook);
        var motionControlDistanceFromAnchor = Vector3.Distance(anchorPosition, motionControlPosition);
        float effectWeight;
        if (motionControlDistanceFromAnchor < hookDistanceFromAnchor)
        {
            effectWeight = 1f;
        }
        else if (falloffJSON.val == 0)
        {
            effectWeight = 0f;
        }
        else
        {
            var distanceFromAnchorHook = Vector3.Distance(anchorHook, motionControlPosition);
            if (distanceFromAnchorHook > falloffJSON.val)
                effectWeight = 0f;
            else
                effectWeight = 1f - (Mathf.Clamp(distanceFromAnchorHook, 0, falloffJSON.val) / falloffJSON.val);
        }

        var realOffset = Vector3.Lerp(upper.realLifeOffset, lower.realLifeOffset, lowerWeight);
        var inGameSize = Vector3.Lerp(upper.inGameSize, lower.inGameSize, lowerWeight);
        var scale = new Vector3(inGameSize.x / realLifeSize.x, 1f, inGameSize.z / realLifeSize.z);
        var actualRelativePosition = motionControlPosition - anchorPosition;
        var scaled = Vector3.Scale(actualRelativePosition, scale);
        var finalPosition = Vector3.Lerp(motionControlPosition, anchorPosition + scaled - realOffset, effectWeight);

        visualCueLinePoints[0] = finalPosition;
        visualCueLinePoints[1] = motionControlPosition;

        // TODO: Compute for both anchors and average them using smooth lerp
        var finalPositionToControlPoint = finalPosition + (hand.motionControl.possessPointTransform.position - motionControlPosition);

        if (previewSnugOffsetJSON.val)
        {
            visualCueLinePoints[0] = motionControl.currentMotionControl.position;
            visualCueLinePoints[1] = finalPosition;
        }

        hand.controllerRigidbody.MovePosition(finalPositionToControlPoint);
        hand.controllerRigidbody.MoveRotation(motionControl.possessPointTransform.rotation);
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

            anchorPoint.inGameCue = new ControllerAnchorPointVisualCue(anchorPoint.rigidBody.transform, Color.gray);
            anchorPoint.Update();

            anchorPoint.realLifeCue = new ControllerAnchorPointVisualCue(anchorPoint.rigidBody.transform, Color.white);
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

        falloffJSON.StoreJSON(jc);
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

        falloffJSON.RestoreFromJSON(jc);
        disableSelfGrabJSON.RestoreFromJSON(jc);
    }

    #endregion
}
