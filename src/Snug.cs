/* TODO
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
public class Snug : MVRScript, ISnug
{
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
    private JSONStorableFloat _anchorVirtSizeXJSON, _anchorVirtSizeZJSON;
    private JSONStorableFloat _anchorVirtOffsetXJSON, _anchorVirtOffsetYJSON, _anchorVirtOffsetZJSON;
    private JSONStorableFloat _anchorPhysOffsetXJSON, _anchorPhysOffsetYJSON, _anchorPhysOffsetZJSON;
    private JSONStorableFloat _anchorPhysSizeXJSON, _anchorPhysSizeZJSON;
    private JSONStorableBool _anchorActiveJSON;
    private readonly Vector3[] _lHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private readonly Vector3[] _rHandVisualCueLinePoints = new Vector3[VisualCueLineIndices.Count];
    private LineRenderer _lHandVisualCueLine, _rHandVisualCueLine;
    private readonly List<GameObject> _lHandVisualCueLinePointIndicators = new List<GameObject>();
    private readonly List<GameObject> _rHandVisualCueLinePointIndicators = new List<GameObject>();
    private InteropProxy _interop;

    private static class VisualCueLineIndices
    {
        public static int Anchor = 0;
        public static int Hand = 1;
        public static int Controller = 2;
        public static int Count = 3;
    }

    public override void Init()
    {
        try
        {
            _interop = new InteropProxy(this, containingAtom);
            _interop.Init();

            if (containingAtom?.type != "Person")
            {
                enabledJSON.val = false;
                return;
            }

            CreateButton("Setup Wizard").button.onClick.AddListener(() => StartCoroutine(Wizard()));

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

            // TODO: Auto move eye target against mirror (to make the model look at herself)
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(Init)}: {exc}");
        }

        StartCoroutine(DeferredInit());
    }

    private IEnumerator Wizard()
    {
        yield return 0;

        var realLeftHand = SuperController.singleton.leftHand;
        var realRightHand = SuperController.singleton.rightHand;
        var headControl = containingAtom.freeControllers.First(fc => fc.name == "headControl");
        var lFootControl = containingAtom.freeControllers.First(fc => fc.name == "lFootControl");
        var rFootControl = containingAtom.freeControllers.First(fc => fc.name == "rFootControl");

        SuperController.singleton.worldScale = 1f;

        AutoSetup();

        // TODO: Use overlays instead
        // TODO: Try and make the model stand straight, not sure how I can do that
        SuperController.singleton.helpText = "Welcome to Embody's Wizard! Stand straight, and press select when ready. Make sure the VR person is also standing straight.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var realHeight = SuperController.singleton.heightAdjustTransform.InverseTransformPoint(SuperController.singleton.centerCameraTarget.transform.position).y;
        // NOTE: Floor is more precise but foot allows to be at non-zero height for calibration
        var gameHeight = headControl.transform.position.y - ((lFootControl.transform.position.y + rFootControl.transform.position.y) / 2f);
        var scale = gameHeight / realHeight;
        SuperController.singleton.worldScale = scale;
        SuperController.LogMessage($"Player height: {realHeight}, model height: {gameHeight}, scale: {scale}");

        SuperController.singleton.helpText = "World scale adjusted. Now put your hands together like you're praying, and press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var handsDistance = Vector3.Distance(realLeftHand.position, realRightHand.position);
        SuperController.LogMessage($"Hand distance: {handsDistance}");

        SuperController.singleton.helpText = "Hand distance recorded. We will now start possession. Press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        // TODO: Replace this by home made possession
        HeadPossess(headControl, true, true, true);

        SuperController.singleton.helpText = "Possession activated. Now put your hands on your real hips, and press select when ready.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        // TODO: Highlight the ring where we want the hands to be.
        var hipsAnchorPoint = _anchorPoints.First(a => a.Label == "Hips");
        var gameHipsCenter = hipsAnchorPoint.GetInGameWorldPosition();
        // TODO: Check the forward size too, and the offset.
        // TODO: Don't check the _hand control_ distance, instead check the relevant distance (from inside the hands)
        var realHipsWidth = Vector3.Distance(realLeftHand.position, realRightHand.position) - handsDistance;
        var realHipsXCenter = (realLeftHand.position + realRightHand.position) / 2f;
        hipsAnchorPoint.RealLifeSize = new Vector3(realHipsWidth, 0f, hipsAnchorPoint.InGameSize.z);
        hipsAnchorPoint.RealLifeOffset = realHipsXCenter - gameHipsCenter;
        SuperController.LogMessage($"Real Hips height: {realHipsXCenter.y}, Game Hips height: {gameHipsCenter}");
        SuperController.LogMessage($"Real Hips width: {realHipsWidth}, Game Hips width: {hipsAnchorPoint.RealLifeSize.x}");
        SuperController.LogMessage($"Real Hips center: {realHipsXCenter}, Game Hips center: {gameHipsCenter}");

        SuperController.singleton.helpText = "Now put your right hand at the same level as your hips but on the front, squeezed on you.";
        while (!AreAnyStartRecordKeysDown()) yield return 0; yield return 0;

        var adjustedHipsCenter = hipsAnchorPoint.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(realRightHand.position, adjustedHipsCenter, handsDistance / 2f);
        hipsAnchorPoint.RealLifeSize = new Vector3(hipsAnchorPoint.RealLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        hipsAnchorPoint.Update();
        _possessHandsJSON.val = true;

        SuperController.singleton.helpText = "All done! Select Possess with Snug in Embody to enable possession with body proportion adjustments.";
        yield return new WaitForSeconds(3);
        SuperController.singleton.helpText = "";
    }

    private static bool AreAnyStartRecordKeysDown()
    {
        var sctrl = SuperController.singleton;
        if (sctrl.isOVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.Touch)) return true;
        }
        if (sctrl.isOpenVR)
        {
            if (sctrl.selectAction.stateDown) return true;
        }
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        return false;
    }

    private void HeadPossess(FreeControllerV3 headPossess, bool alignRig = false, bool usePossessorSnapPoint = true, bool adjustSpring = true)
    {
        // TODO: New
        var motionControllerHead = SuperController.singleton.centerCameraTarget.transform;
        FreeControllerV3 headPossessedController;
        var headPossessedActivateTransform = SuperController.singleton.headPossessedActivateTransform;
        var headPossessedText = SuperController.singleton.headPossessedText;
        var _allowPossessSpringAdjustment = true;
        var _possessPositionSpring = 10000f;
        var _possessRotationSpring = 1000f;

        // TODO: Taken from VaM, to cleanup
        if (!headPossess.canGrabPosition && !headPossess.canGrabRotation)
        {
            return;
        }

        Possessor component = motionControllerHead.GetComponent<Possessor>();
        Rigidbody component2 = motionControllerHead.GetComponent<Rigidbody>();
        headPossessedController = headPossess;
        if (headPossessedActivateTransform != null)
        {
            headPossessedActivateTransform.gameObject.SetActive(true);
        }

        if (headPossessedText != null)
        {
            if (headPossessedController.containingAtom != null)
            {
                headPossessedText.text = headPossessedController.containingAtom.uid + ":" + headPossessedController.name;
            }
            else
            {
                headPossessedText.text = headPossessedController.name;
            }
        }

        headPossessedController.possessed = true;
        if (headPossessedController.canGrabPosition)
        {
            MotionAnimationControl component3 = headPossessedController.GetComponent<MotionAnimationControl>();
            if (component3 != null)
            {
                component3.suspendPositionPlayback = true;
            }

            if (_allowPossessSpringAdjustment && adjustSpring)
            {
                headPossessedController.RBHoldPositionSpring = _possessPositionSpring;
            }
        }

        if (headPossessedController.canGrabRotation)
        {
            MotionAnimationControl component4 = headPossessedController.GetComponent<MotionAnimationControl>();
            if (component4 != null)
            {
                component4.suspendRotationPlayback = true;
            }

            if (_allowPossessSpringAdjustment && adjustSpring)
            {
                headPossessedController.RBHoldRotationSpring = _possessRotationSpring;
            }
        }

        SuperController.singleton.SyncMonitorRigPosition();
        if (alignRig)
        {
            AlignRigAndController(headPossessedController);
        }
        else if (component != null && component.autoSnapPoint != null && usePossessorSnapPoint)
        {
            headPossessedController.PossessMoveAndAlignTo(component.autoSnapPoint);
        }

        if (!(component2 != null))
        {
            return;
        }

        FreeControllerV3.SelectLinkState linkState = FreeControllerV3.SelectLinkState.Position;
        if (headPossessedController.canGrabPosition)
        {
            if (headPossessedController.canGrabRotation)
            {
                linkState = FreeControllerV3.SelectLinkState.PositionAndRotation;
            }
        }
        else if (headPossessedController.canGrabRotation)
        {
            linkState = FreeControllerV3.SelectLinkState.Rotation;
        }

        headPossessedController.SelectLinkToRigidbody(component2, linkState);
    }

    private void AlignRigAndController(FreeControllerV3 controller)
        {
            // TODO: New
            var motionControllerHead = SuperController.singleton.centerCameraTarget.transform;
            FreeControllerV3 headPossessedController;
            var navigationRig = SuperController.singleton.navigationRig;
            var MonitorCenterCamera = SuperController.singleton.MonitorCenterCamera;
            var _allowPossessSpringAdjustment = true;
            var _possessPositionSpring = 10000f;
            var _possessRotationSpring = 1000f;

            // TODO: Taken from VaM, to cleanup
            Possessor component = motionControllerHead.GetComponent<Possessor>();
            Vector3 forwardPossessAxis = controller.GetForwardPossessAxis();
            Vector3 upPossessAxis = controller.GetUpPossessAxis();
            Vector3 up = navigationRig.up;
            Vector3 fromDirection = Vector3.ProjectOnPlane(motionControllerHead.forward, up);
            Vector3 vector = Vector3.ProjectOnPlane(forwardPossessAxis, navigationRig.up);
            if (Vector3.Dot(upPossessAxis, up) < 0f && Vector3.Dot(motionControllerHead.up, up) > 0f)
            {
                vector = -vector;
            }

            Quaternion lhs = Quaternion.FromToRotation(fromDirection, vector);
            navigationRig.rotation = lhs * navigationRig.rotation;
            if (controller.canGrabRotation)
            {
                controller.AlignTo(component.autoSnapPoint, true);
            }

            Vector3 a = (!(controller.possessPoint != null)) ? controller.control.position : controller.possessPoint.position;
            Vector3 b = a - component.autoSnapPoint.position;
            Vector3 vector2 = navigationRig.position + b;
            float num = Vector3.Dot(vector2 - navigationRig.position, up);
            vector2 += up * (0f - num);
            navigationRig.position = vector2;
            SuperController.singleton.playerHeightAdjust += num;
            if (MonitorCenterCamera != null)
            {
                MonitorCenterCamera.transform.LookAt(controller.transform.position + forwardPossessAxis);
                Vector3 localEulerAngles = MonitorCenterCamera.transform.localEulerAngles;
                localEulerAngles.y = 0f;
                localEulerAngles.z = 0f;
                MonitorCenterCamera.transform.localEulerAngles = localEulerAngles;
            }

            controller.PossessMoveAndAlignTo(component.autoSnapPoint);
        }

        private void AutoSetup()
    {
        // TODO: Recalculate when the y offset is changed
        // TODO: Check when the person scale changes
        var colliders = ScanBodyColliders().ToList();
        foreach (var anchor in _anchorPoints)
        {
            if (anchor.Locked) continue;
            // if (anchor.Label != "Abdomen") continue;
            AutoSetup(anchor.RigidBody, anchor, colliders);
        }
        SyncSelectedAnchorJSON(null);
    }

    private void AutoSetup(Rigidbody rb, ControllerAnchorPoint anchor, List<Collider> colliders)
    {
        const float raycastDistance = 100f;
        var rbTransform = rb.transform;
        var rbUp = rbTransform.up;
        var rbOffsetPosition = rbTransform.position + rbUp * anchor.InGameOffset.y;
        var rbRotation = rbTransform.rotation;
        var rbForward = rbTransform.forward;

        var rays = new List<Ray>();
        for (var i = 0; i < 360; i += 5)
        {
            var rotation = Quaternion.AngleAxis(i, rbUp);
            var origin = rbOffsetPosition + rotation * (rbForward * raycastDistance);
            rays.Add(new Ray(origin, rbOffsetPosition - origin));
        }

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        var isHit = false;
        foreach (var collider in colliders)
        {
            foreach (var ray in rays)
            {
                RaycastHit hit;
                if (!collider.Raycast(ray, out hit, raycastDistance)) continue;
                isHit = true;
                min = Vector3.Min(min, hit.point);
                max = Vector3.Max(max, hit.point);

                // var hitCue = VisualCuesHelper.CreatePrimitive(null, PrimitiveType.Cube, new Color(0f, 1f, 0f, 0.2f));
                // _cues.Add(hitCue);
                // hitCue.transform.localScale = Vector3.one * 0.002f;
                // hitCue.transform.position = hit.point;
            }
        }

        if (!isHit) return;

        var size = Quaternion.Inverse(rbRotation) * (max - min);
        var center = min + (max - min) / 2f;
        // TODO: Why add virtual offset here?
        var offset = center - rbTransform.position; //* + rbUp * anchor.VirtualOffset.y;

        // var cue = VisualCuesHelper.Cross(Color.red);
        // _cues.Add(cue);
        // cue.transform.localScale = Vector3.one * 2f;
        // cue.transform.position = center;

        // TODO: Adjust padding for scale?
        var padding = new Vector3(0.02f, 0f, 0.02f);

        anchor.InGameSize = size + padding;
        anchor.InGameOffset = offset;
        anchor.RealLifeSize = anchor.InGameSize;
        anchor.RealLifeOffset = Vector3.zero;
        anchor.Update();
    }

    private IEnumerable<Collider> ScanBodyColliders()
    {
        var personRoot = containingAtom.transform.Find("rescale2");
        // Those are the ones where colliders can actually be found for female:
        //.Find("geometry").Find("FemaleMorphers")
        //.Find("PhysicsModel").Find("Genesis2Female")
        return ScanBodyColliders(personRoot);
    }

    private IEnumerable<Collider> ScanBodyColliders(Transform root)
    {
        if(root.name == "lCollar" || root.name == "rCollar") yield break;
        if(root.name == "lShin" || root.name == "rShin") yield break;

        foreach (var collider in root.GetComponents<Collider>())
        {
            if (!collider.enabled) continue;
            yield return collider;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            foreach (var collider in ScanBodyColliders(child))
                yield return collider;
        }
    }

    private void InitPossessHandsUI()
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

        _possessHandsJSON = new JSONStorableBool("Possess Hands", false, (bool val) =>
        {
            if (!_ready) return;
            if (val)
            {
                if (_leftAutoSnapPoint != null)
                    _leftHandActive = true;
                if (_rightAutoSnapPoint != null)
                    _rightHandActive = true;
                StartCoroutine(DeferredActivateHands());
            }
            else
            {
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
        }) {isStorable = false};
        RegisterBool(_possessHandsJSON);
        CreateToggle(_possessHandsJSON);
    }

    private void InitVisualCuesUI()
    {
        _showVisualCuesJSON = new JSONStorableBool("Show Visual Cues", false, (bool val) =>
        {
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

    private void InitArmForRecordUI()
    {
        CreateButton("Arm hands for record").button.onClick.AddListener(() =>
        {
            SuperController.singleton.ArmAllControlledControllersForRecord();
            _personLHandController.GetComponent<MotionAnimationControl>().armedForRecord = true;
            _personRHandController.GetComponent<MotionAnimationControl>().armedForRecord = true;
        });
    }

    private struct FreeControllerV3Snapshot
    {
        public bool canGrabPosition;
        public bool canGrabRotation;
    }

    private readonly Dictionary<FreeControllerV3, FreeControllerV3Snapshot> _previousState = new Dictionary<FreeControllerV3, FreeControllerV3Snapshot>();

    private void InitDisableSelectionUI()
    {
        var disableSelectionJSON = new JSONStorableBool("Make controllers unselectable", false, (bool val) =>
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
        CreateToggle(disableSelectionJSON);
    }

    private void InitPresetUI()
    {
        var loadPresetUI = CreateButton("Load Preset", false);
        loadPresetUI.button.onClick.AddListener(() =>
        {
            FileManagerSecure.CreateDirectory(_saveFolder);
            var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_saveFolder);
            SuperController.singleton.GetMediaPathDialog((string path) =>
            {
                if (string.IsNullOrEmpty(path)) return;
                JSONClass jc = (JSONClass) LoadJSON(path);
                RestoreFromJSON(jc);
                SyncSelectedAnchorJSON("");
                SyncHandsOffset();
            }, _saveExt, _saveFolder, false, true, false, null, false, shortcuts);
        });

        var savePresetUI = CreateButton("Save Preset", false);
        savePresetUI.button.onClick.AddListener(() =>
        {
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
            fileBrowserUI.Show((string path) =>
            {
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

    private void InitHandsSettingsUI()
    {
        _falloffJSON = new JSONStorableFloat("Effect Falloff", 0.3f, UpdateHandsOffset, 0f, 5f, false) {isStorable = false};
        CreateSlider(_falloffJSON, false);

        _handOffsetXJSON = new JSONStorableFloat("Hand Offset X", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_handOffsetXJSON, false);
        _handOffsetYJSON = new JSONStorableFloat("Hand Offset Y", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_handOffsetYJSON, false);
        _handOffsetZJSON = new JSONStorableFloat("Hand Offset Z", 0f, UpdateHandsOffset, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_handOffsetZJSON, false);

        _handRotateXJSON = new JSONStorableFloat("Hand Rotate X", 0f, UpdateHandsOffset, -25f, 25f, false) {isStorable = false};
        CreateSlider(_handRotateXJSON, false);
        _handRotateYJSON = new JSONStorableFloat("Hand Rotate Y", 0f, UpdateHandsOffset, -25f, 25f, false) {isStorable = false};
        CreateSlider(_handRotateYJSON, false);
        _handRotateZJSON = new JSONStorableFloat("Hand Rotate Z", 0f, UpdateHandsOffset, -25f, 25f, false) {isStorable = false};
        CreateSlider(_handRotateZJSON, false);
    }

    private void InitAnchors()
    {
        var defaultSize = new Vector3(0.2f, 0.2f, 0.2f);
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Head",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "head"),
            InGameOffset = new Vector3(0, 0.2f, 0),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true,
            Locked = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Lips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
            InGameOffset = new Vector3(0, 0, -0.07113313f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Chest",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
            InGameOffset = new Vector3(0, 0.0682705f, 0.04585214f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Abdomen",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
            InGameOffset = new Vector3(0, 0.0770329f, 0.04218798f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Hips",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "hip"),
            InGameOffset = new Vector3(0, -0.08762675f, -0.009161186f),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true
        });
        _anchorPoints.Add(new ControllerAnchorPoint
        {
            Label = "Ground (Control)",
            RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
            InGameOffset = new Vector3(0, 0, 0),
            InGameSize = defaultSize,
            RealLifeOffset = Vector3.zero,
            RealLifeSize = defaultSize,
            Active = true,
            Locked = true
        });
    }

    private void InitAnchorsUI()
    {
        _selectedAnchorsJSON = new JSONStorableStringChooser("Selected Anchor", _anchorPoints.Select(a => a.Label).ToList(), "Chest", "Anchor", SyncSelectedAnchorJSON)
            {isStorable = false};
        CreateScrollablePopup(_selectedAnchorsJSON, true);

        _anchorPhysSizeXJSON = new JSONStorableFloat("Real-Life Size X", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorPhysSizeXJSON, true);
        _anchorPhysSizeZJSON = new JSONStorableFloat("Real-Life Size Z", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorPhysSizeZJSON, true);

        _anchorPhysOffsetXJSON = new JSONStorableFloat("Real-Life Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetXJSON, true);
        _anchorPhysOffsetYJSON = new JSONStorableFloat("Real-Life Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetYJSON, true);
        _anchorPhysOffsetZJSON = new JSONStorableFloat("Real-Life Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorPhysOffsetZJSON, true);

        _anchorVirtSizeXJSON = new JSONStorableFloat("In-Game Size X", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};
        CreateSlider(_anchorVirtSizeXJSON, true);
        _anchorVirtSizeZJSON = new JSONStorableFloat("In-Game Size Z", 1f, UpdateAnchor, 0.01f, 1f, true) {isStorable = false};

        CreateSlider(_anchorVirtSizeZJSON, true);
        _anchorVirtOffsetXJSON = new JSONStorableFloat("In-Game Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetXJSON, true);
        _anchorVirtOffsetYJSON = new JSONStorableFloat("In-Game Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetYJSON, true);
        _anchorVirtOffsetZJSON = new JSONStorableFloat("In-Game Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true) {isStorable = false};
        CreateSlider(_anchorVirtOffsetZJSON, true);

        _anchorActiveJSON = new JSONStorableBool("Anchor Active", true, (bool _) => UpdateAnchor(0)) {isStorable = false};
        CreateToggle(_anchorActiveJSON, true);
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
        // TODO: Not if already loaded
        while (SuperController.singleton.isLoading)
            yield return 0;
        try
        {
            AutoSetup();
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
            json["Hands"] = new JSONClass
            {
                {"Offset", SerializeVector3(_palmToWristOffset)},
                {"Rotation", SerializeVector3(_handRotateOffset)}
            };
            var anchors = new JSONClass();
            foreach (var anchor in _anchorPoints)
            {
                anchors[anchor.Label] = new JSONClass
                {
                    {"InGameOffset", SerializeVector3(anchor.InGameOffset)},
                    {"InGameSize", SerializeVector2(new Vector3(anchor.InGameSize.x, anchor.InGameSize.z))},
                    {"RealLifeOffset", SerializeVector3(anchor.RealLifeOffset)},
                    {"RealLifeScale", SerializeVector2(new Vector2(anchor.RealLifeSize.x, anchor.RealLifeSize.z))},
                    {"Active", anchor.Active ? "true" : "false"},
                };
            }

            json["Anchors"] = anchors;
            needsStore = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(GetJSON)}:  {exc}");
        }

        return json;
    }

    private static string SerializeVector3(Vector3 v)
    {
        return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string SerializeVector2(Vector2 v)
    {
        return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}";
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

        try
        {
            var handsJSON = jc["Hands"];
            if (handsJSON != null)
            {
                _palmToWristOffset = DeserializeVector3(handsJSON["Offset"], _palmToWristOffset);
                _handRotateOffset = DeserializeVector3(handsJSON["Rotation"], _handRotateOffset);
            }

            var anchorsJSON = jc["Anchors"];
            if (anchorsJSON != null)
            {
                foreach (var anchor in _anchorPoints)
                {
                    var anchorJSON = anchorsJSON[anchor.Label];
                    if (anchorJSON == null) continue;
                    anchor.InGameOffset = DeserializeVector3(anchorJSON["InGameOffset"], anchor.InGameOffset);
                    anchor.InGameSize = DeserializeVector2AsFlatScale(anchorJSON["InGameSize"], anchor.InGameSize);
                    anchor.RealLifeOffset = DeserializeVector3(anchorJSON["RealLifeOffset"], anchor.RealLifeOffset);
                    anchor.RealLifeSize = DeserializeVector2AsFlatScale(anchorJSON["RealLifeScale"], anchor.RealLifeSize);
                    anchor.Active = anchorJSON["Active"]?.Value != "false";
                    anchor.Update();
                }
            }

            _loaded = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(RestoreFromJSON)}: {exc}");
        }
    }

    private static Vector3 DeserializeVector3(string v, Vector3 defaultValue)
    {
        if (string.IsNullOrEmpty(v)) return defaultValue;
        var s = v.Split(',');
        return new Vector3(float.Parse(s[0], CultureInfo.InvariantCulture), float.Parse(s[1], CultureInfo.InvariantCulture), float.Parse(s[2], CultureInfo.InvariantCulture));
    }

    private static Vector3 DeserializeVector2AsFlatScale(string v, Vector3 defaultValue)
    {
        if (string.IsNullOrEmpty(v)) return defaultValue;
        var s = v.Split(',');
        return new Vector3(float.Parse(s[0], CultureInfo.InvariantCulture), 1f, float.Parse(s[1], CultureInfo.InvariantCulture));
    }

    #endregion

    private void SyncSelectedAnchorJSON(string _)
    {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorVirtSizeXJSON.valNoCallback = anchor.InGameSize.x;
        _anchorVirtSizeZJSON.valNoCallback = anchor.InGameSize.z;
        _anchorVirtOffsetXJSON.valNoCallback = anchor.InGameOffset.x;
        _anchorVirtOffsetYJSON.valNoCallback = anchor.InGameOffset.y;
        _anchorVirtOffsetZJSON.valNoCallback = anchor.InGameOffset.z;
        _anchorPhysSizeXJSON.valNoCallback = anchor.RealLifeSize.x;
        _anchorPhysSizeZJSON.valNoCallback = anchor.RealLifeSize.z;
        _anchorPhysOffsetXJSON.valNoCallback = anchor.RealLifeOffset.x;
        _anchorPhysOffsetYJSON.valNoCallback = anchor.RealLifeOffset.y;
        _anchorPhysOffsetZJSON.valNoCallback = anchor.RealLifeOffset.z;
        _anchorActiveJSON.valNoCallback = anchor.Active;
    }

    private void UpdateAnchor(float _)
    {
        if (!_ready) return;
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.InGameSize = new Vector3(_anchorVirtSizeXJSON.val, 1f, _anchorVirtSizeZJSON.val);
        anchor.InGameOffset = new Vector3(_anchorVirtOffsetXJSON.val, _anchorVirtOffsetYJSON.val, _anchorVirtOffsetZJSON.val);
        anchor.RealLifeSize = new Vector3(_anchorPhysSizeXJSON.val, 1f, _anchorPhysSizeZJSON.val);
        anchor.RealLifeOffset = new Vector3(_anchorPhysOffsetXJSON.val, _anchorPhysOffsetYJSON.val, _anchorPhysOffsetZJSON.val);
        if (anchor.Locked && !_anchorActiveJSON.val)
        {
            _anchorActiveJSON.valNoCallback = true;
        }
        else if (anchor.Active != _anchorActiveJSON.val)
        {
            anchor.Active = _anchorActiveJSON.val;
            anchor.Update();
        }

        anchor.Update();
    }

    private void SyncHandsOffset()
    {
        if (!_ready) return;
        _handOffsetXJSON.valNoCallback = _palmToWristOffset.x;
        _handOffsetYJSON.valNoCallback = _palmToWristOffset.y;
        _handOffsetZJSON.valNoCallback = _palmToWristOffset.z;
        _handRotateXJSON.valNoCallback = _handRotateOffset.x;
        _handRotateYJSON.valNoCallback = _handRotateOffset.y;
        _handRotateZJSON.valNoCallback = _handRotateOffset.z;
    }

    private void UpdateHandsOffset(float _)
    {
        if (!_ready) return;
        _palmToWristOffset = new Vector3(_handOffsetXJSON.val, _handOffsetYJSON.val, _handOffsetZJSON.val);
        _handRotateOffset = new Vector3(_handRotateXJSON.val, _handRotateYJSON.val, _handRotateZJSON.val);
    }

    #region Update

    public void Update()
    {
        if (!_ready) return;
        try
        {
            UpdateCueLine(_lHandVisualCueLine, _lHandVisualCueLinePoints, _lHandVisualCueLinePointIndicators);
            UpdateCueLine(_rHandVisualCueLine, _rHandVisualCueLinePoints, _rHandVisualCueLinePointIndicators);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(Update)}: {exc}");
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
        if (!_ready) return;
        try
        {
            if (_leftHandActive || _showVisualCuesJSON.val && !ReferenceEquals(_leftAutoSnapPoint, null) && !ReferenceEquals(_leftHandTarget, null))
                ProcessHand(
                    _leftHandTarget,
                    SuperController.singleton.leftHand,
                    _leftAutoSnapPoint,
                    new Vector3(_palmToWristOffset.x * -1f, _palmToWristOffset.y, _palmToWristOffset.z),
                    new Vector3(_handRotateOffset.x, -_handRotateOffset.y, -_handRotateOffset.z),
                    _lHandVisualCueLinePoints
                );
            if (_rightHandActive || _showVisualCuesJSON.val && !ReferenceEquals(_rightAutoSnapPoint, null) && !ReferenceEquals(_rightHandTarget, null))
                ProcessHand(
                    _rightHandTarget,
                    SuperController.singleton.rightHand,
                    _rightAutoSnapPoint,
                    _palmToWristOffset,
                    _handRotateOffset,
                    _rHandVisualCueLinePoints
                );
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(FixedUpdate)}: {exc}");
            OnDisable();
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
        foreach (var anchorPoint in _anchorPoints)
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

        // Find the weight of both anchors (closest = strongest effect)
        var upperPosition = upper.GetAdjustedWorldPosition();
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
        var falloff = _falloffJSON.val > 0 ? 1f - (Mathf.Clamp(distance - realLifeDistanceFromCenter, 0, _falloffJSON.val) / _falloffJSON.val) : 1f;

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
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(Snug)}.{nameof(OnDisable)}: {exc}");
        }
    }

    public void OnDestroy()
    {
        OnDisable();
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

        foreach (var anchorPoint in _anchorPoints)
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
        foreach (var anchor in _anchorPoints)
        {
            Destroy(anchor.VirtualCue?.gameObject);
            anchor.VirtualCue = null;
            Destroy(anchor.PhysicalCue?.gameObject);
            anchor.PhysicalCue = null;
        }
    }

    #endregion
}
