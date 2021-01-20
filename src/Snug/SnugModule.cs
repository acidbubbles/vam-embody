// TODO: Reverse important adjustment sliders and advanced ones. Hide under an advanced toggle.
// TODO: When loading a preset, overwrite with autosetup immediately

using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ISnugModule : IEmbodyModule
{
    List<ControllerAnchorPoint> anchorPoints { get; }
    JSONStorableFloat falloffJSON { get; }
    JSONStorableBool showVisualCuesJSON { get; }
    JSONStorableBool disableSelectionJSON { get; set; }
}

public class SnugModule : EmbodyModuleBase, ISnugModule
{
    public const string Label = "Snug";

    public override string storeId => "Snug";
    public override string label => Label;

    public ITrackersModule trackers;

    public List<ControllerAnchorPoint> anchorPoints { get; } = new List<ControllerAnchorPoint>();
    public JSONStorableFloat falloffJSON { get; private set; }
    public JSONStorableBool showVisualCuesJSON { get; private set; }
    // TODO: Remove this
    public JSONStorableBool disableSelectionJSON { get; set; }
    private SnugHand _lHand;
    private SnugHand _rHand;
    private SnugAutoSetup _autoSetup;

    private struct FreeControllerV3GrabSnapshot
    {
        public bool canGrabPosition;
        public bool canGrabRotation;
    }
    private readonly Dictionary<FreeControllerV3, FreeControllerV3GrabSnapshot> _previousState = new Dictionary<FreeControllerV3, FreeControllerV3GrabSnapshot>();

    public override void Awake()
    {
        try
        {
            base.Awake();

            _autoSetup = new SnugAutoSetup(context.containingAtom, this);

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
            InitDisableSelection();
            InitHandsSettings();
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(Awake)}: {exc}");
        }
    }

    private void InitVisualCues()
    {
        showVisualCuesJSON = new JSONStorableBool("Show Visual Cues", false, val =>
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

    private void InitDisableSelection()
    {
        disableSelectionJSON = new JSONStorableBool("Make controllers unselectable", false, val =>
        {
            if (val)
            {
                foreach (var fc in containingAtom.freeControllers)
                {
                    if (fc == _lHand.controller || fc == _rHand.controller) continue;
                    if (!fc.canGrabPosition && !fc.canGrabRotation) continue;
                    var state = new FreeControllerV3GrabSnapshot {canGrabPosition = fc.canGrabPosition, canGrabRotation = fc.canGrabRotation};
                    _previousState[fc] = state;
                    fc.canGrabPosition = false;
                    fc.canGrabRotation = false;
                }
            }
            else
            {
                foreach (var kvp in _previousState)
                {
                    kvp.Key.canGrabPosition = kvp.Value.canGrabPosition;
                    kvp.Key.canGrabRotation = kvp.Value.canGrabRotation;
                }

                _previousState.Clear();
            }
        });
    }

    private void InitHandsSettings()
    {
        falloffJSON = new JSONStorableFloat("Effect Falloff", 0.3f, 0f, 5f, false) {isStorable = false};
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

    public override void OnEnable()
    {
        base.OnEnable();

        _autoSetup.AutoSetup();

        _lHand.motionControl = trackers.motionControls.FirstOrDefault(mc => mc.name == MotionControlNames.LeftHand);
        if (_lHand.motionControl != null && _lHand.motionControl.SyncMotionControl())
        {
            _lHand.snapshot = FreeControllerV3Snapshot.Snap(_lHand.controller);
            CustomPossessHand(_lHand.controller);
            _lHand.active = true;
            if (showVisualCuesJSON.val) _lHand.showCueLine = true;
        }
        _rHand.motionControl = trackers.motionControls.FirstOrDefault(mc => mc.name == MotionControlNames.RightHand);
        if (_rHand.motionControl != null && _rHand.motionControl.SyncMotionControl())
        {
            _rHand.snapshot = FreeControllerV3Snapshot.Snap(_rHand.controller);
            CustomPossessHand(_rHand.controller);
            _rHand.active = true;
            if (showVisualCuesJSON.val) _rHand.showCueLine = true;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_lHand.active)
        {
            CustomReleaseHand(_lHand.controller);
            _lHand.active = false;
            _lHand.snapshot.Restore();
        }

        if (_rHand.active)
        {
            CustomReleaseHand(_rHand.controller);
            _rHand.active = false;
            _rHand.snapshot.Restore();
        }

        _lHand.showCueLine = false;
        _rHand.showCueLine = false;
    }

    public void OnDestroy()
    {
        OnDisable();
        DestroyVisualCues();
    }

    #endregion

    private static void CustomPossessHand(FreeControllerV3 controllerV3)
    {
        controllerV3.canGrabPosition = false;
        controllerV3.canGrabRotation = false;
        controllerV3.possessed = true;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = true;
    }

    private static void CustomReleaseHand(FreeControllerV3 controllerV3)
    {
        // TODO: This should return to the previous state
        controllerV3.canGrabPosition = true;
        controllerV3.canGrabRotation = true;
        controllerV3.possessed = false;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = false;
    }

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
        var visualCueLinePoints = hand.visualCueLinePoints;
        // Base position
        var position = motionControl.currentMotionControl.position;

        // Find the anchor over and under the controller
        ControllerAnchorPoint lower = null;
        ControllerAnchorPoint upper = null;
        foreach (var anchorPoint in anchorPoints)
        {
            if (!anchorPoint.Active) continue;
            var anchorPointPos = anchorPoint.GetAdjustedWorldPosition();
            if (position.y > anchorPointPos.y && (lower == null || anchorPointPos.y > lower.GetAdjustedWorldPosition().y))
            {
                lower = anchorPoint;
            }
            else if (position.y < anchorPointPos.y && (upper == null || anchorPointPos.y < upper.GetAdjustedWorldPosition().y))
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
        var yUpperDelta = upperPosition.y - position.y;
        var yLowerDelta = position.y - lowerPosition.y;
        var totalDelta = yLowerDelta + yUpperDelta;
        var upperWeight = totalDelta == 0 ? 1f : yLowerDelta / totalDelta;
        var lowerWeight = 1f - upperWeight;
        var lowerRotation = lower.RigidBody.transform.rotation;
        var upperRotation = upper.RigidBody.transform.rotation;
        // TODO: SmoothStep?
        var anchorPosition = Vector3.Lerp(upperPosition, lowerPosition, lowerWeight);
        var anchorRotation = Quaternion.Lerp(upperRotation, lowerRotation, lowerWeight);
        // TODO: Is this useful?
        anchorPosition.y = position.y;

        // Determine the falloff (closer = stronger, fades out with distance)
        // TODO: Even better to use closest point on ellipse, but not necessary.
        var distance = Mathf.Abs(Vector3.Distance(anchorPosition, position));
        var realLifeSize = Vector3.Lerp(upper.RealLifeSize, lower.RealLifeSize, lowerWeight);
        var realLifeDistanceFromCenter = Mathf.Max(realLifeSize.x, realLifeSize.z);
        // TODO: Check both x and z to determine a falloff relative to both distances
        var falloff = falloffJSON.val > 0 ? 1f - (Mathf.Clamp(distance - realLifeDistanceFromCenter, 0, falloffJSON.val) / falloffJSON.val) : 1f;

        // Calculate the controller offset based on the physical scale/offset of anchors
        var realToGameScale = Vector3.Lerp(upper.GetRealToGameScale(), lower.GetRealToGameScale(), lowerWeight);
        var realOffset = Vector3.Lerp(upper.RealLifeOffset, lower.RealLifeOffset, lowerWeight);
        var baseOffset = position - anchorPosition;
        var resultOffset = Quaternion.Inverse(anchorRotation) * baseOffset;
        resultOffset = new Vector3(resultOffset.x / realToGameScale.x, resultOffset.y / realToGameScale.y, resultOffset.z / realToGameScale.z) - realOffset;
        resultOffset = anchorRotation * resultOffset;
        var resultPosition = anchorPosition + Vector3.Lerp(baseOffset, resultOffset, falloff);
        var handPosition = resultPosition + (motionControl.currentMotionControl.InverseTransformPoint(motionControl.possessPointTransform.position));

        // Do the displacement
        hand.controllerRigidbody.MovePosition(handPosition);
        hand.controllerRigidbody.MoveRotation(motionControl.possessPointTransform.rotation);

        visualCueLinePoints[0] = anchorPosition;
        visualCueLinePoints[1] = resultPosition;
    }

    #endregion

    #region Debuggers

    private void CreateVisualCues()
    {
        DestroyVisualCues();

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
    }

    #endregion
}
