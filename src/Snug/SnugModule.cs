/* TODO
- Remove sliders for in-game size, or make them "advanced"
- Run auto setup whenever enabled or visual helpers shown
- When loading a preset, overwrite with autosetup immediately
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface ISnugModule : IEmbodyModule
{
    Vector3 palmToWristOffset { get; set; }
    Vector3 handRotateOffset { get; set; }
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

    public Vector3 palmToWristOffset { get; set; }
    public Vector3 handRotateOffset { get; set; }
    public List<ControllerAnchorPoint> anchorPoints { get; } = new List<ControllerAnchorPoint>();
    // TODO: Oops, this was never saved!
    public JSONStorableFloat falloffJSON { get; private set; }
    public JSONStorableBool showVisualCuesJSON { get; private set; }
    public JSONStorableBool disableSelectionJSON { get; set; }

    private readonly List<GameObject> _cues = new List<GameObject>();
    private bool _leftHandActive, _rightHandActive;
    private GameObject _leftHandTarget, _rightHandTarget;
    private FreeControllerV3 _personLHandController, _personRHandController;
    private Transform _leftAutoSnapPoint, _rightAutoSnapPoint;
    private readonly Vector3[] _lHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private readonly Vector3[] _rHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private LineRenderer _lHandVisualCueLine, _rHandVisualCueLine;
    private readonly List<GameObject> _lHandVisualCueLinePointIndicators = new List<GameObject>();
    private readonly List<GameObject> _rHandVisualCueLinePointIndicators = new List<GameObject>();
    private struct FreeControllerV3Snapshot
    {
        public bool canGrabPosition;
        public bool canGrabRotation;
    }
    private readonly Dictionary<FreeControllerV3, FreeControllerV3Snapshot> _previousState = new Dictionary<FreeControllerV3, FreeControllerV3Snapshot>();

    private static class VisualCueLineIndices
    {
        public static int Anchor = 0;
        public static int Hand = 1;
        public static int Controller = 2;
        public static int Count = 3;
    }

    public override void Awake()
    {
        try
        {
            base.Awake();

            InitAnchors();

            InitHands();
            InitVisualCues();
            InitDisableSelection();
            InitHandsSettings();

            // TODO: Auto move eye target against mirror (to make the model look at herself)
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(Awake)}: {exc}");
        }
    }

    private void InitHands()
    {
        var s = SuperController.singleton;

        Transform touchObjectLeft = null;
        if (s.isOVR)
            touchObjectLeft = s.touchObjectLeft;
        else if (s.isOpenVR)
            touchObjectLeft = s.viveObjectLeft;
        if (touchObjectLeft != null)
            _leftAutoSnapPoint = touchObjectLeft.GetComponent<Possessor>().autoSnapPoint;
        _personLHandController = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "lHandControl");
        if (_personLHandController == null)
            throw new NullReferenceException(
                $"Could not find the lHandControl controller. Controllers: {string.Join(", ", containingAtom.freeControllers.Select(fc => fc.name).ToArray())}");
        _leftHandTarget = new GameObject($"{containingAtom.gameObject.name}.lHandController.snugTarget");
        var leftHandRigidBody = _leftHandTarget.AddComponent<Rigidbody>();
        leftHandRigidBody.isKinematic = true;
        leftHandRigidBody.detectCollisions = false;

        Transform touchObjectRight = null;
        if (s.isOVR)
            touchObjectRight = s.touchObjectRight;
        else if (s.isOpenVR)
            touchObjectRight = s.viveObjectRight;
        if (touchObjectRight != null)
            _rightAutoSnapPoint = touchObjectRight.GetComponent<Possessor>().autoSnapPoint;
        _personRHandController = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "rHandControl");
        if (_personRHandController == null)
            throw new NullReferenceException(
                $"Could not find the rHandControl controller. Controllers: {string.Join(", ", containingAtom.freeControllers.Select(fc => fc.name).ToArray())}");
        _rightHandTarget = new GameObject($"{containingAtom.gameObject.name}.rHandController.snugTarget");
        var rightHandRigidBody = _rightHandTarget.AddComponent<Rigidbody>();
        rightHandRigidBody.isKinematic = true;
        rightHandRigidBody.detectCollisions = false;
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
                    if (fc == _personLHandController || fc == _personRHandController) continue;
                    if (!fc.canGrabPosition && !fc.canGrabRotation) continue;
                    var state = new FreeControllerV3Snapshot {canGrabPosition = fc.canGrabPosition, canGrabRotation = fc.canGrabRotation};
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

    private IEnumerator DeferredActivateHands()
    {
        yield return new WaitForSeconds(0f);
        if (_leftHandActive)
            CustomPossessHand(_personLHandController, _leftHandTarget);
        if (_rightHandActive)
            CustomPossessHand(_personRHandController, _rightHandTarget);
    }

    private static void CustomPossessHand(FreeControllerV3 controllerV3, GameObject target)
    {
        controllerV3.PossessMoveAndAlignTo(target.transform);
        controllerV3.SelectLinkToRigidbody(target.GetComponent<Rigidbody>());
        controllerV3.canGrabPosition = false;
        controllerV3.canGrabRotation = false;
        controllerV3.possessed = true;
        controllerV3.possessable = false;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = true;
    }

    private static void CustomReleaseHand(FreeControllerV3 controllerV3)
    {
        controllerV3.RestorePreLinkState();
        // TODO: This should return to the previous state
        controllerV3.canGrabPosition = true;
        controllerV3.canGrabRotation = true;
        controllerV3.possessed = false;
        controllerV3.possessable = true;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = false;
    }

    #region Load / Save

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);

        jc["Hands"] = new JSONClass
        {
            {"Offset", palmToWristOffset.ToJSON()},
            {"Rotation", handRotateOffset.ToJSON()}
        };
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
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);

        var handsJSON = jc["Hands"];
        if (handsJSON != null)
        {
            palmToWristOffset = handsJSON["Offset"].ToVector3(palmToWristOffset);
            handRotateOffset = handsJSON["Rotation"].ToVector3(handRotateOffset);
        }

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
    }


    #endregion

    #region Update

    public void Update()
    {
        try
        {
            UpdateCueLine(_lHandVisualCueLine, _lHandVisualCueLinePoints, _lHandVisualCueLinePointIndicators);
            UpdateCueLine(_rHandVisualCueLine, _rHandVisualCueLinePoints, _rHandVisualCueLinePointIndicators);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(Update)}: {exc}");
            OnDisable();
        }
    }

    private static void UpdateCueLine(LineRenderer line, Vector3[] points, IList<GameObject> indicators)
    {
        if (line != null)
        {
            line.SetPositions(points);
            for (var i = 0; i < points.Length; i++)
                indicators[i].transform.position = points[i];
        }
    }

    public void FixedUpdate()
    {
        // TODO: The hand should rotate around it's axis, the offset makes the hand rotates
        try
        {
            if (_leftHandActive || showVisualCuesJSON.val && !ReferenceEquals(_leftAutoSnapPoint, null) && !ReferenceEquals(_leftHandTarget, null))
                ProcessHand(
                    _leftHandTarget,
                    SuperController.singleton.leftHand,
                    _leftAutoSnapPoint,
                    new Vector3(palmToWristOffset.x * -1f, palmToWristOffset.y, palmToWristOffset.z),
                    new Vector3(handRotateOffset.x, -handRotateOffset.y, -handRotateOffset.z),
                    _lHandVisualCueLinePoints
                );
            if (_rightHandActive || showVisualCuesJSON.val && !ReferenceEquals(_rightAutoSnapPoint, null) && !ReferenceEquals(_rightHandTarget, null))
                ProcessHand(
                    _rightHandTarget,
                    SuperController.singleton.rightHand,
                    _rightAutoSnapPoint,
                    palmToWristOffset,
                    handRotateOffset,
                    _rHandVisualCueLinePoints
                );
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(SnugModule)}.{nameof(FixedUpdate)}: {exc}");
            enabled = false;
        }
    }

    private void ProcessHand(
        GameObject handTarget,
        Transform realHand,
        Transform autoSnapPoint,
        Vector3 palmToWristOffset,
        Vector3 handRotateOffset,
        Vector3[] visualCueLinePoints
        )
    {
        // Base position
        var position = realHand.position;
        var snapOffset = autoSnapPoint.localPosition;

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
        visualCueLinePoints[VisualCueLineIndices.Anchor] = anchorPosition;

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
        visualCueLinePoints[VisualCueLineIndices.Hand] = resultPosition;

        // Apply the hands adjustments
        var resultRotation = autoSnapPoint.rotation * Quaternion.Euler(handRotateOffset);
        resultPosition += resultRotation * (snapOffset + palmToWristOffset);

        // Do the displacement
        // TODO: Avoid getting the rigidbody component every frame
        var rb = handTarget.GetComponent<Rigidbody>();
        rb.MovePosition(resultPosition);
        rb.MoveRotation(resultRotation);

        visualCueLinePoints[VisualCueLineIndices.Controller] = realHand.transform.position;
    }

    #endregion

    #region Lifecycle

    public override void OnEnable()
    {
        base.OnEnable();

        if (_leftAutoSnapPoint != null)
            _leftHandActive = true;
        if (_rightAutoSnapPoint != null)
            _rightHandActive = true;
        StartCoroutine(DeferredActivateHands());
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_leftHandActive)
        {
            CustomReleaseHand(_personLHandController);
            _leftHandActive = false;
        }

        if (_rightHandActive)
        {
            CustomReleaseHand(_personRHandController);
            _rightHandActive = false;
        }
    }

    public void OnDestroy()
    {
        OnDisable();
        DestroyVisualCues();
        Destroy(_leftHandTarget);
        Destroy(_rightHandTarget);
    }

    #endregion

    #region Debuggers

    private void CreateVisualCues()
    {
        DestroyVisualCues();

        _lHandVisualCueLine = CreateHandVisualCue(_lHandVisualCueLinePointIndicators);
        _rHandVisualCueLine = CreateHandVisualCue(_rHandVisualCueLinePointIndicators);

        foreach (var anchorPoint in anchorPoints)
        {
            if (anchorPoint.Locked) continue;

            anchorPoint.VirtualCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.gray);
            anchorPoint.Update();

            // TODO: Uncomment when there
            anchorPoint.PhysicalCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.white);
            anchorPoint.Update();
        }
    }

    private LineRenderer CreateHandVisualCue(List<GameObject> visualCueLinePointIndicators)
    {
        var lineGo = new GameObject();
        var line = VisualCuesHelper.CreateLine(lineGo, Color.yellow, 0.002f, VisualCueLineIndices.Count, true);
        _cues.Add(lineGo);

        for (var i = 0; i < VisualCueLineIndices.Count; i++)
        {
            var p = VisualCuesHelper.CreatePrimitive(null, PrimitiveType.Cube, Color.yellow);
            p.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            visualCueLinePointIndicators.Add(p);
        }

        return line;
    }

    private void DestroyVisualCues()
    {
        _lHandVisualCueLine = null;
        _rHandVisualCueLine = null;
        foreach (var p in _lHandVisualCueLinePointIndicators)
            Destroy(p);
        _lHandVisualCueLinePointIndicators.Clear();
        foreach (var p in _rHandVisualCueLinePointIndicators)
            Destroy(p);
        _rHandVisualCueLinePointIndicators.Clear();
        foreach (var debugger in _cues)
        {
            Destroy(debugger);
        }

        _cues.Clear();
        foreach (var anchor in anchorPoints)
        {
            Destroy(anchor.VirtualCue?.gameObject);
            anchor.VirtualCue = null;
            Destroy(anchor.PhysicalCue?.gameObject);
            anchor.PhysicalCue = null;
        }
    }

    #endregion
}
