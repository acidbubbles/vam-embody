/* TODO
- Use InGameSize / RealLifeSize to clarify names
- Use size instead of offset
- Remove sliders for in-game size, or make them "advanced"
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Interop;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Snug
/// By Acidbubbles
/// Adjust hand alignement and position to match your actual body
/// Source: https://github.com/acidbubbles/vam-wrist
/// Note: See FixedUpdate for the implementation of the algorithm
/// </summary>
public class Snug : MVRScript, ISnug {
    private const string _saveExt = "snugprofile";
    private const string _saveFolder = "Saves\\snugprofiles";

    private readonly List<GameObject> _cues = new List<GameObject>();
    private readonly List<ControllerAnchorPoint> _anchorPoints = new List<ControllerAnchorPoint>();
    private bool _ready, _loaded;
    private bool _leftHandActive, _rightHandActive;
    private GameObject _leftHandTarget, _rightHandTarget;
    private JSONStorableBool _possessHandsJSON;
    private JSONStorableFloat _falloffJSON;
    private JSONStorableFloat _handOffsetXJSON, _handOffsetYJSON, _handOffsetZJSON;
    private JSONStorableFloat _handRotateXJSON, _handRotateYJSON, _handRotateZJSON;
    private FreeControllerV3 _personLHandController, _personRHandController;
    private Transform _leftAutoSnapPoint, _rightAutoSnapPoint;
    private Vector3 _palmToWristOffset;
    private Vector3 _handRotateOffset;
    private JSONStorableBool _showVisualCuesJSON;
    private JSONStorableStringChooser _selectedAnchorsJSON;
    private JSONStorableFloat _anchorVirtScaleXJSON, _anchorVirtScaleZJSON;
    private JSONStorableFloat _anchorVirtOffsetXJSON, _anchorVirtOffsetYJSON, _anchorVirtOffsetZJSON;
    private JSONStorableFloat _anchorPhysOffsetXJSON, _anchorPhysOffsetYJSON, _anchorPhysOffsetZJSON;
    private JSONStorableFloat _anchorPhysScaleXJSON, _anchorPhysScaleZJSON;
    private JSONStorableBool _anchorActiveJSON;
    private readonly Vector3[] _lHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private readonly Vector3[] _rHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private LineRenderer _lHandVisualCueLine, _rHandVisualCueLine;
    private readonly List<GameObject> _lHandVisualCueLinePointIndicators = new List<GameObject>();
    private readonly List<GameObject> _rHandVisualCueLinePointIndicators = new List<GameObject>();
    private InteropProxy _interop;
    private static class VisualCueLineIndices {
        public static int Anchor = 0;
        public static int Hand = 1;
        public static int Controller = 2;
        public static int Count = 3;
    }

    public override void Init() {
        try {
            _interop = new InteropProxy(this, containingAtom);
            _interop.Init();

            if (containingAtom?.type != "Person")
            {
                enabledJSON.val = false;
                return;
            }

            InitPossessHandsUI();
            InitVisualCuesUI();
            InitArmForRecordUI();
            InitDisableSelectionUI();
            CreateSpacer().height = 10f;
            InitPresetUI();
            CreateSpacer().height = 10f;
            InitHandsSettingsUI();

            InitAnchors();
            InitAnchorsUI();
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(Init)}: {exc}");
        }

        StartCoroutine(DeferredInit());
    }

    private void AutoSetup()
    {
        // TODO: Recalculate when the y offset is changed
        // TODO: Check when the person scale changes
        foreach (var anchor in _anchorPoints)
        {
            if (anchor.Locked) continue;
            // if (anchor.Label != "Abdomen") continue;
            AutoSetup(anchor.RigidBody, anchor);
        }
    }

    private void AutoSetup(Rigidbody rb, ControllerAnchorPoint anchor)
    {
        const float raycastDistance = 100f;
        var rbTransform = rb.transform;
        var rbUp = rbTransform.up;
        var rbOffsetPosition = rbTransform.position + rbUp * anchor.VirtualOffset.y;
        var rbRotation = rbTransform.rotation;
        var rbForward = rbTransform.forward;
        var rbRight = rbTransform.right;

        var rays = new List<Ray>();
        // TODO: Only check front, side and back, no need to check "corners".
        for (var i = 0; i < 360; i += 3)
        {
            var rotation = Quaternion.AngleAxis(i, rbUp);
            var origin = rbOffsetPosition + rotation * (rbForward * raycastDistance);
            rays.Add(new Ray(origin, rbOffsetPosition - origin));
        }
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        var isHit = false;
        foreach (var collider in containingAtom.GetComponentsInChildren<Collider>())
        {
            if (collider.name.EndsWith("Control")) continue;
            if (collider.name.EndsWith("Link")) continue;
            foreach (var ray in rays)
            {
                RaycastHit hit;
                if (!collider.Raycast(ray, out hit, raycastDistance)) continue;
                isHit = true;
                min = Vector3.Min(min, hit.point);
                max = Vector3.Max(max, hit.point);

                var hitCue = VisualCuesHelper.CreatePrimitive(null, PrimitiveType.Cube, new Color(0f, 1f, 0f, 0.2f));
                _cues.Add(hitCue);
                hitCue.transform.localScale = Vector3.one * 0.002f;
                hitCue.transform.position = hit.point;
            }
        }

        if (!isHit) return;

        var size = Quaternion.Inverse(rbRotation) * (max - min);
        var center = min + (max - min) / 2f;
        // TODO: Why add virtual offset here?
        var offset = center - rbTransform.position;//* + rbUp * anchor.VirtualOffset.y;
        // SuperController.LogMessage($"Found max values: {minX:0.000}, {minZ:0.000}, {maxX:0.000}, {maxZ:0.000}");
        // SuperController.LogMessage($"Size: {size.x:0.000}, {size.y:0.000}");
        // SuperController.LogMessage($"Offset: {offset.x:0.000}, {offset.z:0.000}");

        // var cue = VisualCuesHelper.Cross(Color.red);
        // _cues.Add(cue);
        // cue.transform.localScale = Vector3.one * 2f;
        // cue.transform.position = center;

        anchor.VirtualSize = size;
        anchor.VirtualOffset = offset;
        anchor.Update();
    }

    private void InitPossessHandsUI() {
        var s = SuperController.singleton;

        Transform touchObjectLeft = null;
        if (s.isOVR)
            touchObjectLeft = s.touchObjectLeft;
        else if (s.isOpenVR)
            touchObjectLeft = s.viveObjectLeft;
        if (touchObjectLeft != null)
            _leftAutoSnapPoint = touchObjectLeft.GetComponent<Possessor>().autoSnapPoint;
        _personLHandController = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "lHandControl");
        if (_personLHandController == null) throw new NullReferenceException($"Could not find the lHandControl controller. Controllers: {string.Join(", ", containingAtom.freeControllers.Select(fc => fc.name).ToArray())}");
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
        if (_personRHandController == null) throw new NullReferenceException($"Could not find the rHandControl controller. Controllers: {string.Join(", ", containingAtom.freeControllers.Select(fc => fc.name).ToArray())}");
        _rightHandTarget = new GameObject($"{containingAtom.gameObject.name}.rHandController.snugTarget");
        var rightHandRigidBody = _rightHandTarget.AddComponent<Rigidbody>();
        rightHandRigidBody.isKinematic = true;
        rightHandRigidBody.detectCollisions = false;

        _possessHandsJSON = new JSONStorableBool("Possess Hands", false, (bool val) => {
            if (!_ready) return;
            if (val) {
                if (_leftAutoSnapPoint != null)
                    _leftHandActive = true;
                if (_rightAutoSnapPoint != null)
                    _rightHandActive = true;
                StartCoroutine(DeferredActivateHands());
            } else {
                if (_leftHandActive) {
                    CustomReleaseHand(_personLHandController);
                    _leftHandActive = false;
                }
                if (_rightHandActive) {
                    CustomReleaseHand(_personRHandController);
                    _rightHandActive = false;
                }
            }
        }) { isStorable = false };
        RegisterBool(_possessHandsJSON);
        CreateToggle(_possessHandsJSON);
    }

    private void InitVisualCuesUI() {
        // TODO: Make the startingValue false again once this works
        _showVisualCuesJSON = new JSONStorableBool("Show Visual Cues", true, (bool val) => {
            if (!_ready) return;
            if (val)
                CreateVisualCues();
            else
                DestroyVisualCues();
        })
        {
            isStorable = false
        };
        RegisterBool(_showVisualCuesJSON);
        CreateToggle(_showVisualCuesJSON);
    }

    private void InitArmForRecordUI() {
        CreateButton("Arm hands for record").button.onClick.AddListener(() => {
            SuperController.singleton.ArmAllControlledControllersForRecord();
            _personLHandController.GetComponent<MotionAnimationControl>().armedForRecord = true;
            _personRHandController.GetComponent<MotionAnimationControl>().armedForRecord = true;
        });
    }

    private struct FreeControllerV3Snapshot { public bool canGrabPosition; public bool canGrabRotation; }
    private readonly Dictionary<FreeControllerV3, FreeControllerV3Snapshot> _previousState = new Dictionary<FreeControllerV3, FreeControllerV3Snapshot>();

    private void InitDisableSelectionUI() {
        var disableSelectionJSON = new JSONStorableBool("Make controllers unselectable", false, (bool val) => {
            if (val) {
                foreach (var fc in containingAtom.freeControllers) {
                    if (fc == _personLHandController || fc == _personRHandController) continue;
                    if (!fc.canGrabPosition && !fc.canGrabRotation) continue;
                    var state = new FreeControllerV3Snapshot { canGrabPosition = fc.canGrabPosition, canGrabRotation = fc.canGrabRotation };
                    _previousState[fc] = state;
                    fc.canGrabPosition = false;
                    fc.canGrabRotation = false;
                }
            } else {
                foreach (var kvp in _previousState) {
                    kvp.Key.canGrabPosition = kvp.Value.canGrabPosition;
                    kvp.Key.canGrabRotation = kvp.Value.canGrabRotation;
                }
                _previousState.Clear();
            }
        });
        CreateToggle(disableSelectionJSON);
    }

    private void InitPresetUI() {
        var loadPresetUI = CreateButton("Load Preset", false);
        loadPresetUI.button.onClick.AddListener(() => {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_saveFolder);
            SuperController.singleton.GetMediaPathDialog((string path) => {
                if (string.IsNullOrEmpty(path)) return;
                JSONClass jc = (JSONClass)LoadJSON(path);
                RestoreFromJSON(jc);
                SyncSelectedAnchorJSON("");
                SyncHandsOffset();
            }, _saveExt, _saveFolder, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Save Preset", false);
        savePresetUI.button.onClick.AddListener(() => {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var fileBrowserUI = SuperController.singleton.fileBrowserUI;
            fileBrowserUI.SetTitle("Save colliders preset");
            fileBrowserUI.fileRemovePrefix = null;
            fileBrowserUI.hideExtension = false;
            fileBrowserUI.keepOpen = false;
            fileBrowserUI.fileFormat = _saveExt;
            fileBrowserUI.defaultPath = _saveFolder;
            fileBrowserUI.showDirs = true;
            fileBrowserUI.shortCuts = null;
            fileBrowserUI.browseVarFilesAsDirectories = false;
            fileBrowserUI.SetTextEntry(true);
            fileBrowserUI.Show((string path) => {
                fileBrowserUI.fileFormat = null;
                if (string.IsNullOrEmpty(path)) return;
                if (!path.ToLower().EndsWith($".{_saveExt}")) path += $".{_saveExt}";
                var jc = GetJSON();
                jc.Remove("id");
                SaveJSON(jc, path);
            });
            fileBrowserUI.ActivateFileNameField();
        });
    }

    private void InitHandsSettingsUI() {
        _falloffJSON = new JSONStorableFloat("Effect Falloff", 0.3f, UpdateHandsOffset, 0f, 5f, false) { isStorable = false };
        CreateSlider(_falloffJSON, false);

        _handOffsetXJSON = new JSONStorableFloat("Hand Offset X", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
        CreateSlider(_handOffsetXJSON, false);
        _handOffsetYJSON = new JSONStorableFloat("Hand Offset Y", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
        CreateSlider(_handOffsetYJSON, false);
        _handOffsetZJSON = new JSONStorableFloat("Hand Offset Z", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) { isStorable = false };
        CreateSlider(_handOffsetZJSON, false);

        _handRotateXJSON = new JSONStorableFloat("Hand Rotate X", 0f, UpdateHandsOffset, -25f, 25f, false) { isStorable = false };
        CreateSlider(_handRotateXJSON, false);
        _handRotateYJSON = new JSONStorableFloat("Hand Rotate Y", 0f, UpdateHandsOffset, -25f, 25f, false) { isStorable = false };
        CreateSlider(_handRotateYJSON, false);
        _handRotateZJSON = new JSONStorableFloat("Hand Rotate Z", 0f, UpdateHandsOffset, -25f, 25f, false) { isStorable = false };
        CreateSlider(_handRotateZJSON, false);
    }

    private void InitAnchors() {
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Head",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "head"),
            VirtualOffset = new Vector3(0, 0.2f, 0),
            VirtualSize = new Vector3(0.2f, 0.2f, 0.2f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true,
            Locked = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Lips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
            VirtualOffset = new Vector3(0, 0, -0.07113313f),
            VirtualSize = new Vector3(0.05f, 0.05f, 0.05f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Chest",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
            VirtualOffset = new Vector3(0, 0.0682705f, 0.04585214f),
            VirtualSize = new Vector3(0.05f, 0.05f, 0.05f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Abdomen",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
            VirtualOffset = new Vector3(0, 0.0770329f, 0.04218798f),
            VirtualSize = new Vector3(0.05f, 0.05f, 0.05f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Hips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "hip"),
            VirtualOffset = new Vector3(0, -0.08762675f, -0.009161186f),
            VirtualSize = new Vector3(0.05f, 0.05f, 0.05f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint {
            Label = "Ground (Control)",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
            VirtualOffset = new Vector3(0, 0, 0),
            VirtualSize = new Vector3(0.2f, 0.2f, 0.2f),
            PhysicalOffset = new Vector3(0, 0, 0),
            PhysicalSize = new Vector3(1, 1, 1),
            Active = true,
            Locked = true
        });
    }

    private void InitAnchorsUI() {
        _selectedAnchorsJSON = new JSONStorableStringChooser("Selected Anchor", _anchorPoints.Select(a => a.Label).ToList(), "Chest", "Anchor", SyncSelectedAnchorJSON) { isStorable = false };
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

        _anchorActiveJSON = new JSONStorableBool("Anchor Active", true, (bool _) => UpdateAnchor(0)) { isStorable = false };
        CreateToggle(_anchorActiveJSON, true);
    }

    private IEnumerator DeferredActivateHands() {
        yield return new WaitForSeconds(0f);
        if (_leftHandActive)
            CustomPossessHand(_personLHandController, _leftHandTarget);
        if (_rightHandActive)
            CustomPossessHand(_personRHandController, _rightHandTarget);
    }

    private static void CustomPossessHand(FreeControllerV3 controllerV3, GameObject target) {
        controllerV3.PossessMoveAndAlignTo(target.transform);
        controllerV3.SelectLinkToRigidbody(target.GetComponent<Rigidbody>());
        controllerV3.canGrabPosition = false;
        controllerV3.canGrabRotation = false;
        controllerV3.possessed = true;
        controllerV3.possessable = false;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = true;
    }

    private static void CustomReleaseHand(FreeControllerV3 controllerV3) {
        controllerV3.RestorePreLinkState();
        // TODO: This should return to the previous state
        controllerV3.canGrabPosition = true;
        controllerV3.canGrabRotation = true;
        controllerV3.possessed = false;
        controllerV3.possessable = true;
        (controllerV3.GetComponent<HandControl>() ?? controllerV3.GetComponent<HandControlLink>().handControl).possessed = false;
    }

    private IEnumerator DeferredInit() {
        yield return new WaitForEndOfFrame();
        try {
            if (!_loaded) containingAtom.RestoreFromLast(this);
            OnEnable();
            SyncSelectedAnchorJSON("");
            SyncHandsOffset();
            if (_showVisualCuesJSON.val) CreateVisualCues();
            AutoSetup();
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(DeferredInit)}: {exc}");
        }
    }

    #region Load / Save

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false) {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);

        try {
            json["hands"] = new JSONClass{
                {"offset", SerializeVector3(_palmToWristOffset)},
                {"rotation", SerializeVector3(_handRotateOffset)}
            };
            var anchors = new JSONClass();
            foreach (var anchor in _anchorPoints) {
                anchors[anchor.Label] = new JSONClass
                {
                    {"virOffset", SerializeVector3(anchor.VirtualOffset)},
                    {"virScale", SerializeVector2(new Vector3(anchor.VirtualSize.x, anchor.VirtualSize.z))},
                    {"phyOffset", SerializeVector3(anchor.PhysicalOffset)},
                    {"phyScale", SerializeVector2(new Vector2(anchor.PhysicalSize.x, anchor.PhysicalSize.z))},
                    {"active", anchor.Active ? "true" : "false"},
                };
            }
            json["anchors"] = anchors;
            needsStore = true;
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(GetJSON)}:  {exc}");
        }

        return json;
    }

    private static string SerializeVector3(Vector3 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}"; }
    private static string SerializeVector2(Vector2 v) { return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}"; }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true) {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try {
            var handsJSON = jc["hands"];
            if (handsJSON != null) {
                _palmToWristOffset = DeserializeVector3(handsJSON["offset"], _palmToWristOffset);
                _handRotateOffset = DeserializeVector3(handsJSON["rotation"], _handRotateOffset);
            }

            var anchorsJSON = jc["anchors"];
            if (anchorsJSON != null) {
                foreach (var anchor in _anchorPoints) {
                    var anchorJSON = anchorsJSON[anchor.Label];
                    if (anchorJSON == null) continue;
                    anchor.VirtualOffset = DeserializeVector3(anchorJSON["virOffset"], anchor.VirtualOffset);
                    anchor.VirtualSize = DeserializeVector2AsFlatScale(anchorJSON["virScale"], anchor.VirtualSize);
                    anchor.PhysicalOffset = DeserializeVector3(anchorJSON["phyOffset"], anchor.PhysicalOffset);
                    anchor.PhysicalSize = DeserializeVector2AsFlatScale(anchorJSON["phyScale"], anchor.PhysicalSize);
                    anchor.Active = anchorJSON["active"]?.Value != "false";
                    anchor.Update();
                }
            }

            _loaded = true;
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    private static Vector3 DeserializeVector3(string v, Vector3 defaultValue) {
        if (string.IsNullOrEmpty(v)) return defaultValue;
        var s = v.Split(',');
        return new Vector3(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture), float.Parse(s[2], CultureInfo.InvariantCulture));
    }
    private static Vector3 DeserializeVector2AsFlatScale(string v, Vector3 defaultValue) {
        if (string.IsNullOrEmpty(v)) return defaultValue;
        var s = v.Split(',');
        return new Vector3(float.Parse(s[0], CultureInfo.InvariantCulture), 1f, float.Parse(s[1], CultureInfo.InvariantCulture));
    }

    #endregion

    private void SyncSelectedAnchorJSON(string _) {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorVirtScaleXJSON.valNoCallback = anchor.VirtualSize.x;
        _anchorVirtScaleZJSON.valNoCallback = anchor.VirtualSize.z;
        _anchorVirtOffsetXJSON.valNoCallback = anchor.VirtualOffset.x;
        _anchorVirtOffsetYJSON.valNoCallback = anchor.VirtualOffset.y;
        _anchorVirtOffsetZJSON.valNoCallback = anchor.VirtualOffset.z;
        _anchorPhysScaleXJSON.valNoCallback = anchor.PhysicalSize.x;
        _anchorPhysScaleZJSON.valNoCallback = anchor.PhysicalSize.z;
        _anchorPhysOffsetXJSON.valNoCallback = anchor.PhysicalOffset.x;
        _anchorPhysOffsetYJSON.valNoCallback = anchor.PhysicalOffset.y;
        _anchorPhysOffsetZJSON.valNoCallback = anchor.PhysicalOffset.z;
        _anchorActiveJSON.valNoCallback = anchor.Active;
    }

    private void UpdateAnchor(float _) {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.VirtualSize = new Vector3(_anchorVirtScaleXJSON.val, 1f, _anchorVirtScaleZJSON.val);
        anchor.VirtualOffset = new Vector3(_anchorVirtOffsetXJSON.val, _anchorVirtOffsetYJSON.val, _anchorVirtOffsetZJSON.val);
        anchor.PhysicalSize = new Vector3(_anchorPhysScaleXJSON.val, 1f, _anchorPhysScaleZJSON.val);
        anchor.PhysicalOffset = new Vector3(_anchorPhysOffsetXJSON.val, _anchorPhysOffsetYJSON.val, _anchorPhysOffsetZJSON.val);
        if (anchor.Locked && !_anchorActiveJSON.val) {
            _anchorActiveJSON.valNoCallback = true;
        } else if (anchor.Active != _anchorActiveJSON.val) {
            anchor.Active = _anchorActiveJSON.val;
            anchor.Update();
        }
        anchor.Update();
    }

    private void SyncHandsOffset() {
        if (!_ready) return;
        _handOffsetXJSON.valNoCallback = _palmToWristOffset.x;
        _handOffsetYJSON.valNoCallback = _palmToWristOffset.y;
        _handOffsetZJSON.valNoCallback = _palmToWristOffset.z;
        _handRotateXJSON.valNoCallback = _handRotateOffset.x;
        _handRotateYJSON.valNoCallback = _handRotateOffset.y;
        _handRotateZJSON.valNoCallback = _handRotateOffset.z;
    }

    private void UpdateHandsOffset(float _) {
        if (!_ready) return;
        _palmToWristOffset = new Vector3(_handOffsetXJSON.val, _handOffsetYJSON.val, _handOffsetZJSON.val);
        _handRotateOffset = new Vector3(_handRotateXJSON.val, _handRotateYJSON.val, _handRotateZJSON.val);
    }

    #region Update

    public void Update() {
        if (!_ready) return;
        try {
            UpdateCueLine(_lHandVisualCueLine, _lHandVisualCueLinePoints, _lHandVisualCueLinePointIndicators);
            UpdateCueLine(_rHandVisualCueLine, _rHandVisualCueLinePoints, _rHandVisualCueLinePointIndicators);
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(Update)}: {exc}");
            OnDisable();
        }
    }

    private static void UpdateCueLine(LineRenderer line, Vector3[] points, IList<GameObject> indicators) {
        if (line != null) {
            line.SetPositions(points);
            for (var i = 0; i < points.Length; i++)
                indicators[i].transform.position = points[i];
        }
    }

    public void FixedUpdate() {
        if (!_ready) return;
        try {
            if (_leftHandActive || _showVisualCuesJSON.val && _leftAutoSnapPoint != null)
                ProcessHand(_leftHandTarget, SuperController.singleton.leftHand, _leftAutoSnapPoint, new Vector3(_palmToWristOffset.x * -1f, _palmToWristOffset.y, _palmToWristOffset.z), new Vector3(_handRotateOffset.x, -_handRotateOffset.y, -_handRotateOffset.z), _lHandVisualCueLinePoints);
            if (_rightHandActive || _showVisualCuesJSON.val && _rightAutoSnapPoint != null)
                ProcessHand(_rightHandTarget, SuperController.singleton.rightHand, _rightAutoSnapPoint, _palmToWristOffset, _handRotateOffset, _rHandVisualCueLinePoints);
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(FixedUpdate)}: {exc}");
            OnDisable();
        }
    }

    private void ProcessHand(GameObject handTarget, Transform physicalHand, Transform autoSnapPoint, Vector3 palmToWristOffset, Vector3 handRotateOffset, Vector3[] visualCueLinePoints) {
        // Base position
        var position = physicalHand.position;
        var snapOffset = autoSnapPoint.localPosition;

        // Find the anchor over and under the controller
        ControllerAnchorPoint lower = null;
        ControllerAnchorPoint upper = null;
        foreach (var anchorPoint in _anchorPoints) {
            if (!anchorPoint.Active) continue;
            var anchorPointPos = anchorPoint.GetWorldPosition();
            if (position.y > anchorPointPos.y && (lower == null || anchorPointPos.y > lower.GetWorldPosition().y)) {
                lower = anchorPoint;
            } else if (position.y < anchorPointPos.y && (upper == null || anchorPointPos.y < upper.GetWorldPosition().y)) {
                upper = anchorPoint;
            }
        }

        if (lower == null)
            lower = upper;
        else if (upper == null)
            upper = lower;

        // Find the weight of both anchors (closest = strongest effect)
        var upperPosition = upper.GetWorldPosition();
        var lowerPosition = lower.GetWorldPosition();
        var yUpperDelta = upperPosition.y - position.y;
        var yLowerDelta = position.y - lowerPosition.y;
        var totalDelta = yLowerDelta + yUpperDelta;
        var upperWeight = totalDelta == 0 ? 1f : yLowerDelta / totalDelta;
        var lowerWeight = 1f - upperWeight;
        var lowerRotation = lower.RigidBody.transform.rotation;
        var upperRotation = upper.RigidBody.transform.rotation;
        var anchorPosition = Vector3.Lerp(upperPosition, lowerPosition, lowerWeight);
        var anchorRotation = Quaternion.Lerp(upperRotation, lowerRotation, lowerWeight);
        // TODO: Is this useful?
        anchorPosition.y = position.y;
        visualCueLinePoints[VisualCueLineIndices.Anchor] = anchorPosition;

        // Determine the falloff (closer = stronger, fades out with distance)
        // TODO: Even better to use closest point on ellipse, but not necessary.
        var distance = Mathf.Abs(Vector3.Distance(anchorPosition, position));
        var physicalCueSize = Vector3.zero; // TODO: Bring this back correctly Vector3.Lerp(Vector3.Scale(upper.VirtualSize, upper.PhysicalSize), Vector3.Scale(lower.VirtualSize, lower.PhysicalSize), lowerWeight) * (_baseCueSize / 2f);
        var physicalCueDistanceFromCenter = Mathf.Max(physicalCueSize.x, physicalCueSize.z);
        // TODO: Check both x and z to determine a falloff relative to both distances
        var falloff = _falloffJSON.val > 0 ? 1f - (Mathf.Clamp(distance - physicalCueDistanceFromCenter, 0, _falloffJSON.val) / _falloffJSON.val) : 1f;

        // Calculate the controller offset based on the physical scale/offset of anchors
        var physicalScale = Vector3.Lerp(upper.PhysicalSize, lower.PhysicalSize, lowerWeight);
        var physicalOffset = Vector3.Lerp(upper.PhysicalOffset, lower.PhysicalOffset, lowerWeight);
        var baseOffset = position - anchorPosition;
        var resultOffset = Quaternion.Inverse(anchorRotation) * baseOffset;
        resultOffset = new Vector3(resultOffset.x / physicalScale.x, resultOffset.y / physicalScale.y, resultOffset.z / physicalScale.z) - physicalOffset;
        resultOffset = anchorRotation * resultOffset;
        var resultPosition = anchorPosition + Vector3.Lerp(baseOffset, resultOffset, falloff);
        visualCueLinePoints[VisualCueLineIndices.Hand] = resultPosition;

        // Apply the hands adjustments
        var resultRotation = autoSnapPoint.rotation * Quaternion.Euler(handRotateOffset);
        resultPosition += resultRotation * (snapOffset + palmToWristOffset);

        // Do the displacement
        var rb = handTarget.GetComponent<Rigidbody>();
        rb.MovePosition(resultPosition);
        rb.MoveRotation(resultRotation);

        visualCueLinePoints[VisualCueLineIndices.Controller] = physicalHand.transform.position;

        // SuperController.singleton.ClearMessages();
        // SuperController.LogMessage($"y {position.y:0.00} btwn {lower.RigidBody.name} y {yLowerDelta:0.00} w {lowerWeight:0.00} and {upper.RigidBody.name} y {yUpperDelta:0.00} w {upperWeight: 0.00}");
        // SuperController.LogMessage($"dist {distance:0.00}/{physicalCueDistanceFromCenter:0.00} falloff {falloff:0.00}");
        // SuperController.LogMessage($"rot {upperRotation.eulerAngles} psca {physicalScale} poff {physicalOffset} base {baseOffset} res {resultOffset}");
    }

    #endregion

    #region Lifecycle

    public void OnEnable()
    {
        if (containingAtom == null) return;
        if (containingAtom.type != "Person")
        {
            enabledJSON.val = false;
            return;
        }

        try {
            _ready = true;
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(OnEnable)}: {exc}");
        }
    }

    public void OnDisable() {
        try {
            _ready = false;
            DestroyVisualCues();
            Destroy(_rightHandTarget);
        } catch (Exception exc) {
            SuperController.LogError($"{nameof(Snug)}.{nameof(OnDisable)}: {exc}");
        }
    }

    public void OnDestroy() {
        OnDisable();
    }

    #endregion

    #region Debuggers

    private void CreateVisualCues() {
        DestroyVisualCues();

        // TODO: Uncomment when there
        // _lHandVisualCueLine = CreateHandVisualCue(_lHandVisualCueLinePointIndicators);
        // _rHandVisualCueLine = CreateHandVisualCue(_rHandVisualCueLinePointIndicators);

        foreach (var anchorPoint in _anchorPoints) {
            anchorPoint.VirtualCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.gray);
            anchorPoint.Update();

            // TODO: Uncomment when there
            // anchorPoint.PhysicalCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.white);
            // anchorPoint.Update();
        }
    }

    private LineRenderer CreateHandVisualCue(List<GameObject> visualCueLinePointIndicators) {
        var lineGo = new GameObject();
        var line = VisualCuesHelper.CreateLine(lineGo, Color.yellow, 0.002f, VisualCueLineIndices.Count, true);
        _cues.Add(lineGo);

        for (var i = 0; i < VisualCueLineIndices.Count; i++) {
            var p = VisualCuesHelper.CreatePrimitive(null, PrimitiveType.Cube, Color.yellow);
            p.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            visualCueLinePointIndicators.Add(p);
        }

        return line;
    }

    private void DestroyVisualCues() {
        _lHandVisualCueLine = null;
        _rHandVisualCueLine = null;
        foreach (var p in _lHandVisualCueLinePointIndicators)
            Destroy(p);
        _lHandVisualCueLinePointIndicators.Clear();
        foreach (var p in _rHandVisualCueLinePointIndicators)
            Destroy(p);
        _rHandVisualCueLinePointIndicators.Clear();
        foreach (var debugger in _cues) {
            Destroy(debugger);
        }
        _cues.Clear();
        foreach (var anchor in _anchorPoints) {
            Destroy(anchor.VirtualCue?.gameObject);
            anchor.VirtualCue = null;
            Destroy(anchor.PhysicalCue?.gameObject);
            anchor.PhysicalCue = null;
        }
    }

    #endregion
}
