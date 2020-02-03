using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Snug
/// By Acidbubbles
/// Better controller alignement when possessing
/// Source: https://github.com/acidbubbles/vam-wrist
/// </summary>
public class Snug : MVRScript
{
    private readonly List<GameObject> _cues = new List<GameObject>();
    private readonly List<ControllerAnchorPoint> _anchorPoints = new List<ControllerAnchorPoint>();
    private bool _ready, _loaded;
    private GameObject _leftHandTarget, _rightHandTarget;
    private JSONStorableBool _possessHandsJSON;
    public JSONStorableFloat _handOffsetXJSON, _handOffsetYJSON, _handOffsetZJSON;
    public JSONStorableFloat _handRotateXJSON, _handRotateYJSON, _handRotateZJSON;
    private FreeControllerV3 _personLHandController, _personRHandController;
    private Transform _leftAutoSnapPoint, _rightAutoSnapPoint;
    private Vector3 _palmToWristOffset;
    private Quaternion _handRotateOffset = Quaternion.Euler(0, 0, 0);
    private JSONStorableBool _showVisualCuesJSON;
    private JSONStorableStringChooser _selectedAnchorsJSON;
    private JSONStorableFloat _anchorVirtScaleXJSON, _anchorVirtScaleZJSON;
    private JSONStorableFloat _anchorVirtOffsetXJSON, _anchorVirtOffsetYJSON, _anchorVirtOffsetZJSON;
    private JSONStorableFloat _anchorPhysOffsetXJSON, _anchorPhysOffsetYJSON, _anchorPhysOffsetZJSON;
    private JSONStorableFloat _anchorPhysScaleXJSON, _anchorPhysScaleZJSON;

    public override void Init()
    {
        try
        {
            if (containingAtom.type != "Person")
            {
                SuperController.LogError("Snug can only be applied on Person atoms.");
                return;
            }

            var s = SuperController.singleton;

            Transform touchObjectLeft = null;
            if (s.isOVR)
                touchObjectLeft = s.touchObjectLeft;
            else if (s.isOpenVR)
                touchObjectLeft = s.viveObjectLeft;
            if (touchObjectLeft != null)
                _leftAutoSnapPoint = touchObjectLeft.GetComponent<Possessor>().autoSnapPoint;
            _personLHandController = containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
            if (_personLHandController == null) throw new NullReferenceException("Could not find the lHandControl controller");
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
            _personRHandController = containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
            if (_personRHandController == null) throw new NullReferenceException("Could not find the rHandControl controller");
            _rightHandTarget = new GameObject($"{containingAtom.gameObject.name}.rHandController.snugTarget");
            var rightHandRigidBody = _rightHandTarget.AddComponent<Rigidbody>();
            rightHandRigidBody.isKinematic = true;
            rightHandRigidBody.detectCollisions = false;

            _possessHandsJSON = new JSONStorableBool("Possess Hands", false, (bool val) =>
            {
                if (!_ready) return;
                CustomPossessHand(_personLHandController, _leftHandTarget, val);
                CustomPossessHand(_personRHandController, _rightHandTarget, val);
            })
            { isStorable = false };
            RegisterBool(_possessHandsJSON);
            CreateToggle(_possessHandsJSON);

            _showVisualCuesJSON = new JSONStorableBool("Show Visual Cues", false, (bool val) =>
            {
                if (!_ready) return;
                if (val)
                    CreateVisualCues();
                else
                    DestroyVisualCues();
            });
            RegisterBool(_showVisualCuesJSON);
            CreateToggle(_showVisualCuesJSON);

            {
                GameObject lastRbCue = null;
                var rigidBodiesJSON = new JSONStorableStringChooser(
                    "Target",
                    containingAtom.rigidbodies.Select(rb => rb.name).OrderBy(n => n).ToList(), "", "Target",
                    (string val) =>
                    {
                        if (lastRbCue != null)
                        {
                            Destroy(lastRbCue);
                            _cues.Remove(lastRbCue);
                        }

                        var rigidbody = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == val);
                        if (rigidbody == null) return;
                        var rbCue = VisualCuesHelper.Cross(Color.white);
                        rbCue.transform.parent = rigidbody.transform;
                        rbCue.transform.localScale = new Vector3(2f, 2f, 2f);
                        rbCue.transform.localPosition = Vector3.zero;
                        _cues.Add(rbCue);
                        lastRbCue = rbCue;
                    }
                )
                { isStorable = false };
                CreateScrollablePopup(rigidBodiesJSON);
            }

            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Lips",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
                PhysicalOffset = new Vector3(0, 0, 0),
                PhysicalScale = new Vector2(1, 1),
                VirtualOffset = new Vector3(0, 0, 0),
                VirtualScale = new Vector2(1, 1)
            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Chest",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
                PhysicalOffset = new Vector3(0, 0, 0),
                PhysicalScale = new Vector2(1, 1),
                VirtualOffset = new Vector3(0, 0, 0),
                VirtualScale = new Vector2(1, 1)

            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Abdomen",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
                PhysicalOffset = new Vector3(0, 0, 0),
                PhysicalScale = new Vector2(1, 1),
                VirtualOffset = new Vector3(0, 0, 0),
                VirtualScale = new Vector2(1, 1)

            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Hips",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "hip"),
                PhysicalOffset = new Vector3(0, 0, 0),
                PhysicalScale = new Vector2(1, 1),
                VirtualOffset = new Vector3(0, 0, 0),
                VirtualScale = new Vector2(1, 1)

            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Ground (Control)",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
                PhysicalOffset = new Vector3(0, 0, 0),
                PhysicalScale = new Vector2(1, 1),
                VirtualOffset = new Vector3(0, 0, 0),
                VirtualScale = new Vector2(1, 1)

            });

            _selectedAnchorsJSON = new JSONStorableStringChooser("Selected Anchor", _anchorPoints.Select(a => a.Label).ToList(), _anchorPoints[0].Label, "Anchor", SyncSelectedAnchorJSON) { isStorable = false };
            CreateScrollablePopup(_selectedAnchorsJSON, true);

            _anchorVirtScaleXJSON = new JSONStorableFloat("Virtual Scale X", 1f, UpdateAnchor, 0.01f, 3f, true) { isStorable = false };
            CreateSlider(_anchorVirtScaleXJSON, true);
            _anchorVirtScaleZJSON = new JSONStorableFloat("Virtual Scale Z", 1f, UpdateAnchor, 0.01f, 3f, true) { isStorable = false };

            CreateSlider(_anchorVirtScaleZJSON, true);
            _anchorVirtOffsetXJSON = new JSONStorableFloat("Virtual Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorVirtOffsetXJSON, true);
            _anchorVirtOffsetYJSON = new JSONStorableFloat("Virtual Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorVirtOffsetYJSON, true);
            _anchorVirtOffsetZJSON = new JSONStorableFloat("Virtual Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorVirtOffsetZJSON, true);

            _anchorPhysScaleXJSON = new JSONStorableFloat("Physical Scale X", 1f, UpdateAnchor, 0.01f, 3f, true) { isStorable = false };
            CreateSlider(_anchorPhysScaleXJSON, true);
            _anchorPhysScaleZJSON = new JSONStorableFloat("Physical Scale Z", 1f, UpdateAnchor, 0.01f, 3f, true) { isStorable = false };
            CreateSlider(_anchorPhysScaleZJSON, true);

            _anchorPhysOffsetXJSON = new JSONStorableFloat("Physical Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorPhysOffsetXJSON, true);
            _anchorPhysOffsetYJSON = new JSONStorableFloat("Physical Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorPhysOffsetYJSON, true);
            _anchorPhysOffsetZJSON = new JSONStorableFloat("Physical Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_anchorPhysOffsetZJSON, true);

            // TODO: Figure out a simple way to configure this quickly (e.g. two moveable controllers, one for the vr body, the other for the real space equivalent)

            _handOffsetXJSON = new JSONStorableFloat("Hand Offset X", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_handOffsetXJSON, false);
            _handOffsetYJSON = new JSONStorableFloat("Hand Offset Y", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_handOffsetYJSON, false);
            _handOffsetZJSON = new JSONStorableFloat("Hand Offset Z", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
            CreateSlider(_handOffsetZJSON, false);

            _handRotateXJSON = new JSONStorableFloat("Hand Rotate X", 0f, UpdateHandsOffset, -25f, 25f, true) { isStorable = false };
            CreateSlider(_handRotateXJSON, false);
            _handRotateYJSON = new JSONStorableFloat("HandWrist Rotate Y", 0f, UpdateHandsOffset, -25f, 25f, true) { isStorable = false };
            CreateSlider(_handRotateYJSON, false);
            _handRotateZJSON = new JSONStorableFloat("HandWrist Rotate Z", 0f, UpdateHandsOffset, -25f, 25f, true) { isStorable = false };
            CreateSlider(_handRotateZJSON, false);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(Init)}: {exc}");
        }

        StartCoroutine(DeferredInit());
    }

    private static void CustomPossessHand(FreeControllerV3 controllerV3, GameObject target, bool active)
    {
        if (active)
        {
            controllerV3.PossessMoveAndAlignTo(target.transform);
            controllerV3.SelectLinkToRigidbody(target.GetComponent<Rigidbody>());
            controllerV3.canGrabPosition = false;
            controllerV3.canGrabRotation = false;
            controllerV3.possessed = false;
            controllerV3.possessable = false;
        }
        else
        {
            controllerV3.RestorePreLinkState();
            // TODO: This should return to the previous state
            controllerV3.canGrabPosition = true;
            controllerV3.canGrabRotation = true;
            controllerV3.possessable = true;
        }
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();
        try
        {
            if (!_loaded) containingAtom.RestoreFromLast(this);
            OnEnable();
            SyncSelectedAnchorJSON("");
            SyncHandsOffset();
            if (_showVisualCuesJSON.val) CreateVisualCues();
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(DeferredInit)}: {exc}");
        }
    }

    #region Load / Save

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);

        try
        {
            json["hands"] = new JSONClass{
                {"offset", SerializeVector3(_palmToWristOffset)},
                {"rotation", SerializeQuaternion(_handRotateOffset)}
            };
            var anchors = new JSONClass();
            foreach (var anchor in _anchorPoints)
            {
                anchors[anchor.Label] = new JSONClass
                {
                    {"offset", SerializeVector3(anchor.PhysicalOffset)},
                    {"scale", SerializeVector2(anchor.PhysicalScale)},
                    {"cueOffset", SerializeVector3(anchor.VirtualOffset)},
                    {"cueScale", SerializeVector2(anchor.VirtualScale)},
                };
            }
            json["anchors"] = anchors;
            needsStore = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(GetJSON)}:  {exc}");
        }

        return json;
    }

    private static string SerializeQuaternion(Quaternion v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}"; }
    private static string SerializeVector3(Vector3 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}"; }
    private static string SerializeVector2(Vector2 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}"; }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            var handsJSON = jc["hands"];
            if (handsJSON != null)
            {
                _palmToWristOffset = DeserializeVector3(handsJSON["offset"]);
                _handRotateOffset = DeserializeQuaternion(handsJSON["rotation"]);
            }

            var anchorsJSON = jc["anchors"];
            if (anchorsJSON != null)
            {
                foreach (var anchor in _anchorPoints)
                {
                    var anchorJSON = anchorsJSON[anchor.Label];
                    if (anchorJSON == null) continue;
                    anchor.PhysicalOffset = DeserializeVector3(anchorJSON["offset"]);
                    anchor.PhysicalScale = DeserializeVector2(anchorJSON["scale"]);
                    anchor.VirtualOffset = DeserializeVector3(anchorJSON["cueOffset"]);
                    anchor.VirtualScale = DeserializeVector2(anchorJSON["cueScale"]);
                }
            }

            _loaded = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    private static Quaternion DeserializeQuaternion(string v) { var s = v.Split(','); return new Quaternion(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture), float.Parse(s[2], CultureInfo.InvariantCulture), float.Parse(s[3], CultureInfo.InvariantCulture)); }
    private static Vector3 DeserializeVector3(string v) { var s = v.Split(','); return new Vector3(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture), float.Parse(s[2], CultureInfo.InvariantCulture)); }
    private static Vector2 DeserializeVector2(string v) { var s = v.Split(','); return new Vector2(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture)); }

    #endregion

    private void SyncSelectedAnchorJSON(string _)
    {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorVirtScaleXJSON.valNoCallback = anchor.VirtualScale.x;
        _anchorVirtScaleZJSON.valNoCallback = anchor.VirtualScale.y;
        _anchorVirtOffsetXJSON.valNoCallback = anchor.VirtualOffset.x;
        _anchorVirtOffsetYJSON.valNoCallback = anchor.VirtualOffset.y;
        _anchorVirtOffsetZJSON.valNoCallback = anchor.VirtualOffset.z;
        _anchorPhysScaleXJSON.valNoCallback = anchor.PhysicalScale.x;
        _anchorPhysScaleZJSON.valNoCallback = anchor.PhysicalScale.y;
        _anchorPhysOffsetXJSON.valNoCallback = anchor.PhysicalOffset.x;
        _anchorPhysOffsetYJSON.valNoCallback = anchor.PhysicalOffset.y;
        _anchorPhysOffsetZJSON.valNoCallback = anchor.PhysicalOffset.z;
    }

    private void UpdateAnchor(float _)
    {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.VirtualScale = new Vector2(_anchorVirtScaleXJSON.val, _anchorVirtScaleZJSON.val);
        anchor.VirtualOffset = new Vector3(_anchorVirtOffsetXJSON.val, _anchorVirtOffsetYJSON.val, _anchorVirtOffsetZJSON.val);
        anchor.PhysicalScale = new Vector2(_anchorPhysScaleXJSON.val, _anchorPhysScaleZJSON.val);
        anchor.PhysicalOffset = new Vector3(_anchorPhysOffsetXJSON.val, _anchorPhysOffsetYJSON.val, _anchorPhysOffsetZJSON.val);
        anchor.Update();
    }

    private void SyncHandsOffset()
    {
        if (!_ready) return;
        _handOffsetXJSON.valNoCallback = _palmToWristOffset.x;
        _handOffsetYJSON.valNoCallback = _palmToWristOffset.y;
        _handOffsetZJSON.valNoCallback = _palmToWristOffset.z;
        var rotateOffset = _handRotateOffset.eulerAngles;
        _handRotateXJSON.valNoCallback = rotateOffset.x;
        _handRotateYJSON.valNoCallback = rotateOffset.y;
        _handRotateZJSON.valNoCallback = rotateOffset.z;

    }

    private void UpdateHandsOffset(float _)
    {
        if (!_ready) return;
        _palmToWristOffset = new Vector3(_handOffsetXJSON.val, _handOffsetYJSON.val, _handOffsetZJSON.val);
        _handRotateOffset = Quaternion.Euler(_handRotateXJSON.val, _handRotateYJSON.val, _handRotateZJSON.val);
    }

    #region Update

    public void FixedUpdate()
    {
        if (!_ready || _rightAutoSnapPoint == null) return;
        try
        {
            ProcessHand(_rightHandTarget, SuperController.singleton.rightHand, _rightAutoSnapPoint, _palmToWristOffset, _handRotateOffset);
            ProcessHand(_leftHandTarget, SuperController.singleton.leftHand, _leftAutoSnapPoint, new Vector3(_palmToWristOffset.x * -1f, _palmToWristOffset.y, _palmToWristOffset.z), new Quaternion(_handRotateOffset.x * -1f, _handRotateOffset.y, _handRotateOffset.z, _handRotateOffset.z * -1f));
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(FixedUpdate)}: {exc}");
            OnDisable();
        }
    }

    private void ProcessHand(GameObject handTarget, Transform physicalHand, Transform autoSnapPoint, Vector3 palmToWristOffset, Quaternion handRotateOffset)
    {
        if (physicalHand == null || handTarget == null || autoSnapPoint == null) return;

        var position = physicalHand.position;
        var snapOffset = autoSnapPoint.position - position;

        ControllerAnchorPoint lower = null;
        ControllerAnchorPoint upper = null;
        foreach (var anchorPoint in _anchorPoints)
        {
            var anchorPointY = anchorPoint.GetWorldY();
            if (position.y > anchorPointY && (lower == null || anchorPointY > lower.GetWorldY()))
            {
                lower = anchorPoint;
            }
            else if (position.y < anchorPointY && (upper == null || anchorPointY < upper.GetWorldY()))
            {
                upper = anchorPoint;
            }
        }

        if (lower == null)
            lower = upper;
        else if (upper == null)
            upper = lower;

        var yUpperDelta = upper.GetWorldY() - position.y;
        var yLowerDelta = position.y - lower.GetWorldY();
        var totalDelta = yLowerDelta + yUpperDelta;
        var upperWeight = yLowerDelta / totalDelta;
        var lowerWeight = 1f - upperWeight;
        // TODO: Use scale too
        var anchorPosition = ((upper.RigidBody.transform.position + upper.VirtualOffset) * upperWeight) + ((lower.RigidBody.transform.position + lower.VirtualOffset) * lowerWeight);
        var anchorOffset = (upper.PhysicalOffset * upperWeight) + (lower.PhysicalOffset * lowerWeight);
        var anchorRotation = Quaternion.Lerp(lower.RigidBody.transform.rotation, upper.RigidBody.transform.rotation, upperWeight);
        var anchorScale = (upper.PhysicalScale * upperWeight) + (lower.PhysicalScale * lowerWeight);

        SuperController.singleton.ClearMessages();
        SuperController.LogMessage($"Between {lower.RigidBody.name} ({lowerWeight}) and {upper.RigidBody.name} ({upperWeight})");
        SuperController.LogMessage($"Anchor {upper.VirtualOffset.y} and {lower.VirtualOffset.y} = {anchorOffset.y}");

        // TODO: Use this for falloff, based on distance - closest point on the ellipse
        var distance = Mathf.Abs(Vector2.Distance(
            new Vector2(anchorPosition.x, anchorPosition.z),
            new Vector2(position.x, position.z)
        ));

        var resultPosition = position + snapOffset + palmToWristOffset + (anchorRotation * new Vector3(-anchorOffset.x, 0f, -anchorOffset.z));
        resultPosition.x = anchorPosition.x + (resultPosition.x - anchorPosition.x) * (1f / anchorScale.y);
        resultPosition.z = anchorPosition.z + (resultPosition.z - anchorPosition.z) * (1f / anchorScale.x);
        var resultRotation = autoSnapPoint.rotation * handRotateOffset;

        handTarget.transform.SetPositionAndRotation(resultPosition, resultRotation);
    }

    #endregion

    #region Lifecycle

    public void OnEnable()
    {
        try
        {
            _ready = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(OnEnable)}: {exc}");
        }
    }

    public void OnDisable()
    {
        try
        {
            _ready = false;
            DestroyVisualCues();
            Destroy(_rightHandTarget);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(OnDisable)}: {exc}");
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }

    #endregion

    #region Debuggers

    private void CreateVisualCues()
    {
        DestroyVisualCues();

        CreateHandVisualCue(SuperController.singleton.leftHand, _personLHandController, _leftHandTarget);
        CreateHandVisualCue(SuperController.singleton.rightHand, _personRHandController, _rightHandTarget);

        foreach (var anchorPoint in _anchorPoints)
        {
            anchorPoint.VirtualCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.gray);
            anchorPoint.Update();

            anchorPoint.PhysicalCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.white);
            anchorPoint.Update();
        }
    }

    private void CreateHandVisualCue(Transform physicalHand, FreeControllerV3 controllerV3, GameObject target)
    {
        var handCue = VisualCuesHelper.Cross(Color.red);
        handCue.transform.SetPositionAndRotation(
            physicalHand.position,
            physicalHand.rotation
        );
        handCue.transform.parent = physicalHand;
        _cues.Add(handCue);

        if (controllerV3 != null)
        {
            var controllerCue = VisualCuesHelper.Cross(Color.blue);
            controllerCue.transform.SetPositionAndRotation(
                controllerV3.control.position,
                controllerV3.control.rotation
            );
            controllerCue.transform.parent = controllerV3.control;
            _cues.Add(controllerCue);
        }

        var wristCue = VisualCuesHelper.Cross(Color.green);
        wristCue.transform.SetPositionAndRotation(
            target.transform.position,
            target.transform.rotation
        );
        wristCue.transform.parent = target.transform;
        _cues.Add(wristCue);
    }

    private void DestroyVisualCues()
    {
        foreach (var debugger in _cues)
        {
            Destroy(debugger);
        }
        _cues.Clear();
        foreach (var anchor in _anchorPoints)
        {
            Destroy(anchor.VirtualCue?.gameObject);
            anchor.VirtualCue = null;
            Destroy(anchor.PhysicalCue?.gameObject);
            anchor.PhysicalCue = null;
        }
    }

    #endregion

    private class ControllerAnchorPoint
    {
        public string Label { get; internal set; }
        public Rigidbody RigidBody { get; set; }
        public Vector3 PhysicalOffset { get; set; }
        public Vector2 PhysicalScale { get; set; }
        public Vector3 VirtualOffset { get; set; }
        public Vector2 VirtualScale { get; set; }
        public ControllerAnchorPointVisualCue VirtualCue { get; set; }
        public ControllerAnchorPointVisualCue PhysicalCue { get; set; }

        public float GetWorldY()
        {
            return RigidBody.transform.position.y + VirtualOffset.y;
        }

        internal void Update()
        {
            if (PhysicalCue != null)
            {
                VirtualCue.Update(VirtualOffset, VirtualScale);
                PhysicalCue.Update(VirtualOffset + PhysicalOffset, VirtualScale * PhysicalScale);
            }
        }
    }

    private class ControllerAnchorPointVisualCue : IDisposable
    {
        private const float _width = 0.005f;
        public readonly GameObject gameObject;
        private readonly Transform _xAxis;
        private readonly Transform _zAxis;
        private readonly Transform _frontHandle;
        private readonly Transform _leftHandle;
        private readonly Transform _rightHandle;
        private readonly LineRenderer _ellipse;

        public ControllerAnchorPointVisualCue(Transform parent, Color color)
        {
            var go = new GameObject();
            _xAxis = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform;
            _zAxis = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform;
            _frontHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
            _leftHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
            _rightHandle = VisualCuesHelper.CreatePrimitive(go.transform, PrimitiveType.Cube, color).transform;
            _ellipse = VisualCuesHelper.CreateEllipse(go, color, _width);
            if (_ellipse == null) throw new NullReferenceException("Boom");
            gameObject = go;
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
        }

        public void Update(Vector3 offset, Vector2 scale)
        {
            gameObject.transform.localPosition = offset;

            var size = new Vector2(0.35f * scale.x, 0.35f * scale.y);
            _xAxis.localScale = new Vector3(size.x - _width * 2, _width * 0.25f, _width * 0.25f);
            _zAxis.localScale = new Vector3(_width * 0.25f, _width * 0.25f, size.y - _width * 2);
            _frontHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
            _frontHandle.transform.localPosition = Vector3.forward * size.y / 2;
            _leftHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
            _leftHandle.transform.localPosition = Vector3.left * size.x / 2;
            _rightHandle.localScale = new Vector3(_width * 3, _width * 3, _width * 3);
            _rightHandle.transform.localPosition = Vector3.right * size.x / 2;
            if (_ellipse == null) throw new NullReferenceException("Bam");
            VisualCuesHelper.DrawEllipse(_ellipse, new Vector2(size.x / 2f, size.y / 2f));
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }

    private static class VisualCuesHelper
    {
        public static GameObject Cross(Color color)
        {
            var go = new GameObject();
            var size = 0.2f; var width = 0.005f;
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.red).transform.localScale = new Vector3(size, width, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.green).transform.localScale = new Vector3(width, size, width);
            CreatePrimitive(go.transform, PrimitiveType.Cube, Color.blue).transform.localScale = new Vector3(width, width, size);
            CreatePrimitive(go.transform, PrimitiveType.Sphere, color).transform.localScale = new Vector3(size / 8f, size / 8f, size / 8f);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        public static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color, renderQueue = 4000 };
            foreach (var c in go.GetComponents<Collider>()) { c.enabled = false; Destroy(c); }
            go.transform.parent = parent;
            return go;
        }

        public static LineRenderer CreateEllipse(GameObject go, Color color, float width, int resolution = 32)
        {
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.material = new Material(Shader.Find("Sprites/Default")) { renderQueue = 4000 };
            line.widthMultiplier = width;
            line.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }
            };
            line.positionCount = resolution;
            return line;
        }

        public static void DrawEllipse(LineRenderer line, Vector2 radius)
        {
            for (int i = 0; i <= line.positionCount; i++)
            {
                var angle = i / (float)line.positionCount * 2.0f * Mathf.PI;
                Quaternion pointQuaternion = Quaternion.AngleAxis(90, Vector3.right);
                Vector3 pointPosition;

                pointPosition = new Vector3(radius.x * Mathf.Cos(angle), radius.y * Mathf.Sin(angle), 0.0f);
                pointPosition = pointQuaternion * pointPosition;

                line.SetPosition(i, pointPosition);
            }
            line.loop = true;
        }
    }
}
