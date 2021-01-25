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
}

public class SnugModule : EmbodyModuleBase, ISnugModule
{
    public const string Label = "Snug";

    public override string storeId => "Snug";
    public override string label => Label;

    public ITrackersModule trackers;

    public List<ControllerAnchorPoint> anchorPoints { get; } = new List<ControllerAnchorPoint>();
    public JSONStorableFloat falloffJSON { get; private set; }
    public JSONStorableBool previewSnugOffsetJSON { get; private set; }
    public JSONStorableBool disableSelfGrabJSON { get; set; }
    private SnugHand _lHand;
    private SnugHand _rHand;
    public SnugAutoSetup autoSetup { get; private set; }

    private struct FreeControllerV3GrabSnapshot
    {
        public FreeControllerV3 controller;
        public bool canGrabPosition;
        public bool canGrabRotation;
    }
    private readonly List<FreeControllerV3GrabSnapshot> _previousState = new List<FreeControllerV3GrabSnapshot>();

    public override void Awake()
    {
        try
        {
            base.Awake();

            autoSetup = new SnugAutoSetup(context.containingAtom, this);

            InitAnchors();

            _lHand = new SnugHand
            {
                controller = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "lHandControl")
            };
            _rHand = new SnugHand
            {
                controller = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "rHandControl")
            };

            InitVisualCues();
            disableSelfGrabJSON = new JSONStorableBool("DisablePersonGrab", false);
            falloffJSON = new JSONStorableFloat("Effect Falloff", 0.3f, 0f, 5f, false) {isStorable = false};
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
        var defaultSize = new Vector3(0.2f, 0.2f, 0.2f);
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Crown",
            Label = "Crown",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "head"),
            InGameOffset = new Vector3(0, 0.2f, 0),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true,
            Locked = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Lips",
            Label = "Lips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
            InGameOffset = new Vector3(0, 0, -0.07113313f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Chest",
            Label = "Chest",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
            InGameOffset = new Vector3(0, 0.0682705f, 0.04585214f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Abdomen",
            Label = "Abdomen",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
            InGameOffset = new Vector3(0, 0.0770329f, 0.04218798f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Hips",
            Label = "Hips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "hip"),
            InGameOffset = new Vector3(0, -0.08762675f, -0.009161186f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        anchorPoints.Add(new ControllerAnchorPoint
        {
            Id = "Feet",
            Label = "Feet",
            // TODO: We could try and average this instead, OR define an absolute value
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
            InGameOffset = new Vector3(0, 0, 0),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true,
            Locked = true
        });
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
                _previousState.Add(new FreeControllerV3GrabSnapshot {controller = fc, canGrabPosition = fc.canGrabPosition, canGrabRotation = fc.canGrabRotation});
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
        if (trackers.restorePoseAfterPossessJSON.val)
            hand.snapshot = FreeControllerV3Snapshot.Snap(hand.controller);
        _previousState.Add(new FreeControllerV3GrabSnapshot {controller = hand.controller, canGrabPosition = hand.controller.canGrabPosition, canGrabRotation = hand.controller.canGrabRotation});
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
            c.controller.canGrabPosition = c.canGrabPosition;
            c.controller.canGrabRotation = c.canGrabRotation;
        }
        _previousState.Clear();
    }

    private static void DisableHand(SnugHand hand)
    {
        if (!hand.active) return;
        hand.controller.possessed = false;
        (hand.controller.GetComponent<HandControl>() ?? hand.controller.GetComponent<HandControlLink>().handControl).possessed = false;
        hand.active = false;
        if (hand.snapshot != null)
        {
            hand.snapshot.Restore();
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
            if (!anchorPoint.Active) continue;
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
        var lowerRotation = lower.RigidBody.transform.rotation;
        var upperRotation = upper.RigidBody.transform.rotation;
        // TODO: We can use a bezier curve or similar to make a curve following the angle of the upper/lower controls
        var anchorPosition = Vector3.Lerp(upperPosition, lowerPosition, lowerWeight);
        var anchorRotation = Quaternion.Lerp(upperRotation, lowerRotation, lowerWeight);

        var angle = Mathf.Deg2Rad * Vector3.SignedAngle(anchorRotation * Vector3.forward, motionControlPosition - anchorPosition, anchorRotation * Vector3.up);
        var realLifeSize = Vector3.Lerp(upper.RealLifeSize, lower.RealLifeSize, lowerWeight);
        // TODO: This seemed wrong in testing, I could not bring the hand in the expected position
        var anchorHook = anchorPosition + new Vector3(Mathf.Sin(angle) * realLifeSize.x / 2f, 0f, Mathf.Cos(angle) * realLifeSize.z / 2f);

        // TODO: When closer to center it should clamp to 0, otherwise when scales get large it will push hand instead of pulling when too close.
        var distanceFromAnchorHook = Vector3.Distance(anchorHook, motionControlPosition);
        var falloff = falloffJSON.val > 0 ? 1f - (Mathf.Clamp(distanceFromAnchorHook, 0, falloffJSON.val) / falloffJSON.val) : 1f;

        var realOffset = Vector3.Lerp(upper.RealLifeOffset, lower.RealLifeOffset, lowerWeight);
        var inGameSize = Vector3.Lerp(upper.InGameSize, lower.InGameSize, lowerWeight);
        var scale = new Vector3(inGameSize.x / realLifeSize.x, inGameSize.y / realLifeSize.y, inGameSize.z / realLifeSize.z);
        var actualRelativePosition = motionControlPosition - anchorPosition;
        var scaled = Vector3.Scale(actualRelativePosition, scale);
        var finalPosition = Vector3.Lerp(motionControlPosition, anchorPosition + scaled - realOffset, falloff);

        visualCueLinePoints[0] = finalPosition;
        visualCueLinePoints[1] = motionControlPosition;

        var finalPositionToControlPoint = finalPosition + (hand.motionControl.possessPointTransform.position - motionControlPosition);// + hand.motionControl.possessPointTransform.position * hand.motionControl.baseOffset;

        hand.controllerRigidbody.MovePosition(finalPositionToControlPoint);
        hand.controllerRigidbody.MoveRotation(motionControl.possessPointTransform.rotation);

        visualCueLinePoints[0] = motionControl.possessPointTransform.position;
        visualCueLinePoints[1] = finalPositionToControlPoint;
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
            if (anchorPoint.Locked) continue;

            anchorPoint.VirtualCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.gray);
            anchorPoint.Update();

            anchorPoint.PhysicalCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.white);
            anchorPoint.Update();
        }
    }

    private void DestroyVisualCues()
    {
        _lHand.showCueLine = false;
        _rHand.showCueLine = false;

        foreach (var anchor in anchorPoints)
        {
            Destroy(anchor.VirtualCue?.gameObject);
            anchor.VirtualCue = null;
            Destroy(anchor.PhysicalCue?.gameObject);
            anchor.PhysicalCue = null;
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
            anchors[anchor.Id] = new JSONClass
            {
                {"InGameOffset", anchor.InGameOffset.ToJSON()},
                {"InGameSize", anchor.InGameSize.ToJSON()},
                {"RealLifeOffset", anchor.RealLifeOffset.ToJSON()},
                {"RealLifeScale", anchor.RealLifeSize.ToJSON()},
                {"Active", anchor.Active ? "true" : "false"},
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
                var anchorJSON = anchorsJSON[anchor.Id];
                if (anchorJSON == null) continue;
                anchor.InGameOffset = anchorJSON["InGameOffset"].ToVector3(anchor.InGameOffset);
                anchor.InGameSize = anchorJSON["InGameSize"].ToVector3(anchor.InGameSize);
                anchor.RealLifeOffset = anchorJSON["RealLifeOffset"].ToVector3(anchor.RealLifeOffset);
                anchor.RealLifeSize = anchorJSON["RealLifeScale"].ToVector3(anchor.RealLifeSize);
                anchor.Active = anchorJSON["Active"]?.Value != "false";
                anchor.Update();
            }
        }

        falloffJSON.RestoreFromJSON(jc);
        disableSelfGrabJSON.RestoreFromJSON(jc);
    }

    #endregion
}
