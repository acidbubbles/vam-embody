using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Snug
/// By Acidbubbles
/// Better controller alignement when possessing
/// Source: https://github.com/acidbubbles/vam-wrist
/// </summary>
public class Snug : MVRScript
{
    private readonly List<GameObject> _debuggers = new List<GameObject>();
    private readonly List<ControllerAnchorPoint> _anchorPoints = new List<ControllerAnchorPoint>();
    private GameObject _rightHandTarget;
    private JSONStorableBool _possessRightHandJSON;
    public JSONStorableFloat _wristOffsetXJSON;
    private JSONStorableFloat _wristOffsetYJSON;
    private JSONStorableFloat _wristOffsetZJSON;
    public JSONStorableFloat _wristRotateXJSON;
    private JSONStorableFloat _wristRotateYJSON;
    private JSONStorableFloat _wristRotateZJSON;
    private FreeControllerV3 _rightHandController;
    private Transform _rightAutoSnapPoint;
    private bool _ready;
    private Vector3 _palmToWristOffset;
    private Quaternion _rotateOffset;
    private JSONStorableBool _showAnchorsJSON;
    private JSONStorableStringChooser _selectedAnchorsJSON;
    private JSONStorableFloat _anchorCueScaleXJSON;
    private JSONStorableFloat _anchorCueScaleZJSON;
    private JSONStorableFloat _anchorCueOffsetXJSON;
    private JSONStorableFloat _anchorCueOffsetYJSON;
    private JSONStorableFloat _anchorCueOffsetZJSON;
    private JSONStorableFloat _anchorOffsetXJSON;
    private JSONStorableFloat _anchorOffsetYJSON;
    private JSONStorableFloat _anchorOffsetZJSON;
    private JSONStorableFloat _anchorScaleXJSON;
    private JSONStorableFloat _anchorScaleZJSON;

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
            Transform touchObjectRight = null;
            if (s.isOVR)
                touchObjectRight = s.touchObjectRight;
            else if (s.isOpenVR)
                touchObjectRight = s.viveObjectRight;
            else
                SuperController.LogError("Snug hands will only work in VR");
            if (touchObjectRight != null)
                _rightAutoSnapPoint = touchObjectRight.GetComponent<Possessor>().autoSnapPoint;

            _rightHandController = containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
            if (_rightHandController == null) throw new NullReferenceException("Could not find the rHandControl controller");

            _rightHandTarget = new GameObject($"{containingAtom.gameObject.name}.lHandController.snugTarget");
            var rightHandRigidBody = _rightHandTarget.AddComponent<Rigidbody>();
            rightHandRigidBody.isKinematic = true;
            rightHandRigidBody.detectCollisions = false;

            _possessRightHandJSON = new JSONStorableBool("Possess Right Hand", false, (bool val) =>
            {
                if (_rightHandController.possessed)
                {
                    _possessRightHandJSON.valNoCallback = false;
                    SuperController.LogError("Snug: Cannot activate while possessing");
                    return;
                }
                if (val)
                {
                    _rightHandController.PossessMoveAndAlignTo(_rightHandTarget.transform);
                    _rightHandController.SelectLinkToRigidbody(_rightHandTarget.GetComponent<Rigidbody>());
                    _rightHandController.canGrabPosition = false;
                    _rightHandController.canGrabRotation = false;
                    _rightHandController.possessable = false;
                }
                else
                {
                    _rightHandController.RestorePreLinkState();
                    // TODO: This should return to the previous state
                    _rightHandController.canGrabPosition = true;
                    _rightHandController.canGrabRotation = true;
                    _rightHandController.possessable = true;
                }
            });
            RegisterBool(_possessRightHandJSON);
            CreateToggle(_possessRightHandJSON);

            _showAnchorsJSON = new JSONStorableBool("Show Anchors", false, (bool val) =>
            {
                if (val)
                {
                    CreateDebuggers();
                }
                else
                {
                    DestroyDebuggers();
                }
            });
            CreateToggle(_showAnchorsJSON);

            {
                GameObject rbDebugger = null;
                var rigidBodiesJSON = new JSONStorableStringChooser(
                    "Target",
                    containingAtom.rigidbodies.Select(rb => rb.name).OrderBy(n => n).ToList(), "", "Target",
                    (string val) =>
                    {
                        if (rbDebugger != null)
                        {
                            Destroy(rbDebugger);
                            _debuggers.Remove(rbDebugger);
                        }

                        var rigidbody = containingAtom.rigidbodies.FirstOrDefault(rb => rb.name == val);
                        if (rigidbody == null) return;
                        var debugger = VisualCuesHelper.Cross(Color.white);
                        debugger.transform.SetPositionAndRotation(
                            rigidbody.transform.position,
                            rigidbody.transform.rotation
                        );
                        debugger.transform.parent = rigidbody.transform;
                        _debuggers.Add(debugger);
                        rbDebugger = debugger;
                    }
                )
                {
                    isStorable = false
                };
                CreateScrollablePopup(rigidBodiesJSON);
            }

            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Lips",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
                Offset = new Vector3(0, 0.01f, -0.05f),
                Scale = new Vector2(1, 1),
                CueOffset = new Vector3(0, 0, 0),
                CueScale = new Vector2(1, 1)
            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Chest",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
                Offset = new Vector3(0, 0, 0.08f),
                Scale = new Vector2(1, 1),
                CueOffset = new Vector3(0, 0, 0),
                CueScale = new Vector2(1, 1)

            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Abdomen",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
                Offset = new Vector3(0, -0.05f, 0.1f),
                Scale = new Vector2(1, 1),
                CueOffset = new Vector3(0, 0, 0),
                CueScale = new Vector2(1, 1)

            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                Label = "Ground (Control)",
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "object"),
                Offset = new Vector3(0, 0, 0),
                Scale = new Vector2(1, 1),
                CueOffset = new Vector3(0, 0, 0),
                CueScale = new Vector2(1, 1)

            });

            _selectedAnchorsJSON = new JSONStorableStringChooser("Anchor", _anchorPoints.Select(a => a.Label).ToList(), _anchorPoints[0].Label, "Anchor", SyncSelectedAnchorJSON);
            CreateScrollablePopup(_selectedAnchorsJSON, true);

            _anchorCueScaleXJSON = new JSONStorableFloat("Anchor Cue Scale X", 1f, UpdateAnchor, 0.01f, 2f, true);
            CreateSlider(_anchorCueScaleXJSON, true);
            _anchorCueScaleZJSON = new JSONStorableFloat("Anchor Cue Scale Z", 1f, UpdateAnchor, 0.01f, 2f, true);
            CreateSlider(_anchorCueScaleZJSON, true);
            _anchorCueOffsetXJSON = new JSONStorableFloat("Anchor Cue Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            CreateSlider(_anchorCueOffsetXJSON, true);
            _anchorCueOffsetYJSON = new JSONStorableFloat("Anchor Cue Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            CreateSlider(_anchorCueOffsetYJSON, true);
            _anchorCueOffsetZJSON = new JSONStorableFloat("Anchor Cue Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            RegisterFloat(_anchorCueOffsetZJSON);
            CreateSlider(_anchorCueOffsetZJSON, true);

            _anchorOffsetXJSON = new JSONStorableFloat("Anchor Offset X", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            CreateSlider(_anchorOffsetXJSON, true);
            _anchorOffsetYJSON = new JSONStorableFloat("Anchor Offset Y", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            CreateSlider(_anchorOffsetYJSON, true);
            _anchorOffsetZJSON = new JSONStorableFloat("Anchor Offset Z", 0f, UpdateAnchor, -0.2f, 0.2f, true);
            RegisterFloat(_anchorOffsetZJSON);
            CreateSlider(_anchorOffsetZJSON, true);

            _anchorScaleXJSON = new JSONStorableFloat("Anchor Scale X", 1f, UpdateAnchor, 0.01f, 2f, true);
            CreateSlider(_anchorScaleXJSON, true);
            _anchorScaleZJSON = new JSONStorableFloat("Anchor Scale Z", 1f, UpdateAnchor, 0.01f, 2f, true);
            CreateSlider(_anchorScaleZJSON, true);

            SyncSelectedAnchorJSON(_selectedAnchorsJSON.val);

            // TODO: Figure out a simple way to configure this quickly (e.g. two moveable controllers, one for the vr body, the other for the real space equivalent)

            _wristOffsetXJSON = new JSONStorableFloat("Right Wrist Offset X", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            CreateSlider(_wristOffsetXJSON, false);
            _wristOffsetYJSON = new JSONStorableFloat("Right Wrist Offset Y", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            CreateSlider(_wristOffsetYJSON, false);
            _wristOffsetZJSON = new JSONStorableFloat("Right Wrist Offset Z", -0.01f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            CreateSlider(_wristOffsetZJSON, false);

            _wristRotateXJSON = new JSONStorableFloat("Right Wrist Rotate X", 8.33f, UpdateRightHandOffset, -25f, 25f, true);
            CreateSlider(_wristRotateXJSON, false);
            _wristRotateYJSON = new JSONStorableFloat("Right Wrist Rotate Y", 0f, UpdateRightHandOffset, -25f, 25f, true);
            CreateSlider(_wristRotateYJSON, false);
            _wristRotateZJSON = new JSONStorableFloat("Right Wrist Rotate Z", 0f, UpdateRightHandOffset, -25f, 25f, true);
            CreateSlider(_wristRotateZJSON, false);
        }
        catch (Exception exc)
        {
            SuperController.LogError("Snug.Init: " + exc);
        }

        StartCoroutine(DeferredInit());
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();
        try
        {
            UpdateRightHandOffset(0);
            OnEnable();
        }
        catch (Exception exc)
        {
            SuperController.LogError("Snug.DeferredInit: " + exc);
        }
    }

    private void SyncSelectedAnchorJSON(string val)
    {
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        _anchorCueScaleXJSON.valNoCallback = anchor.CueScale.x;
        _anchorCueScaleZJSON.valNoCallback = anchor.CueScale.y;
        _anchorCueOffsetXJSON.valNoCallback = anchor.CueOffset.x;
        _anchorCueOffsetYJSON.valNoCallback = anchor.CueOffset.y;
        _anchorCueOffsetZJSON.valNoCallback = anchor.CueOffset.z;
        _anchorScaleXJSON.valNoCallback = anchor.Scale.x;
        _anchorScaleZJSON.valNoCallback = anchor.Scale.y;
        _anchorOffsetXJSON.valNoCallback = anchor.Offset.x;
        _anchorOffsetYJSON.valNoCallback = anchor.Offset.y;
        _anchorOffsetZJSON.valNoCallback = anchor.Offset.z;
    }

    private void UpdateAnchor(float _)
    {
        var anchor = _anchorPoints.FirstOrDefault(a => a.Label == _selectedAnchorsJSON.val);
        if (anchor == null) throw new NullReferenceException($"Could not find the selected anchor {_selectedAnchorsJSON.val}");
        anchor.CueScale = new Vector2(_anchorCueScaleXJSON.val, _anchorCueScaleZJSON.val);
        anchor.CueOffset = new Vector3(_anchorCueOffsetXJSON.val, _anchorCueOffsetYJSON.val, _anchorCueOffsetZJSON.val);
        anchor.Scale = new Vector2(_anchorScaleXJSON.val, _anchorScaleZJSON.val);
        anchor.Offset = new Vector3(_anchorOffsetXJSON.val, _anchorOffsetYJSON.val, _anchorOffsetZJSON.val);
        anchor.Update();
    }

    private void UpdateRightHandOffset(float _)
    {
        _palmToWristOffset = new Vector3(_wristOffsetXJSON.val, _wristOffsetYJSON.val, _wristOffsetZJSON.val);
        _rotateOffset = Quaternion.Euler(_wristRotateXJSON.val, _wristRotateYJSON.val, _wristRotateZJSON.val);
    }

    #region Update

    public void Update()
    {
        if (!_ready) return;
        try
        {
        }
        catch (Exception exc)
        {
            SuperController.LogError("Snug.Update: " + exc);
            OnDisable();
        }
    }

    private struct WeightedVector3
    {
        public Vector3 vector;
        public float weight;
    }

    public void FixedUpdate()
    {
        if (!_ready || _rightAutoSnapPoint == null) return;
        try
        {
            var snapOffset = _rightAutoSnapPoint.position - SuperController.singleton.rightHand.position;
            var position = SuperController.singleton.rightHand.position;

            ControllerAnchorPoint lower = null;
            ControllerAnchorPoint upper = null;
            foreach (var anchorPoint in _anchorPoints)
            {
                var anchorPointY = anchorPoint.RigidBody.transform.position.y;
                if (position.y > anchorPointY && (lower == null || anchorPointY > lower.RigidBody.transform.position.y))
                {
                    lower = anchorPoint;
                }
                else if (position.y < anchorPointY && (upper == null || anchorPointY < upper.RigidBody.transform.position.y))
                {
                    upper = anchorPoint;
                }
            }

            if (lower == null || upper == null)
            {
                // TODO: Add fake points with zero scale/offset
                // TODO: Add a lower effect based on distance
                SuperController.singleton.ClearMessages();
                SuperController.LogMessage($"Between {lower?.RigidBody.name ?? "(none)"} and {upper?.RigidBody.name ?? "(none)"}");
                _rightHandTarget.transform.SetPositionAndRotation(
                    position + snapOffset + _palmToWristOffset,
                    _rightAutoSnapPoint.rotation * _rotateOffset
                );
                return;
            }

            var yUpperDelta = upper.RigidBody.transform.position.y - position.y;
            var yLowerDelta = position.y - lower.RigidBody.transform.position.y;
            var totalDelta = yLowerDelta + yUpperDelta;
            var upperWeight = yLowerDelta / totalDelta;
            var lowerWeight = 1f - upperWeight;
            var anchorOffset = (upper.Offset * upperWeight) + (lower.Offset * lowerWeight);
            var anchorRotation = Quaternion.Lerp(lower.RigidBody.transform.rotation, upper.RigidBody.transform.rotation, upperWeight);

            SuperController.singleton.ClearMessages();
            SuperController.LogMessage($"Between {lower.RigidBody.name} ({lowerWeight}) and {upper.RigidBody.name} ({upperWeight})");
            SuperController.LogMessage($"Offset {lower.Offset} and {upper.Offset} = {anchorOffset}");

            _rightHandTarget.transform.SetPositionAndRotation(
                position + snapOffset + _palmToWristOffset + (anchorRotation * anchorOffset),
                _rightAutoSnapPoint.rotation * _rotateOffset
            );
        }
        catch (Exception exc)
        {
            SuperController.LogError("Snug.FixedUpdate: " + exc);
            OnDisable();
        }
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
            SuperController.LogError("Snug.OnEnable: " + exc);
        }
    }

    public void OnDisable()
    {
        try
        {
            _ready = false;
            DestroyDebuggers();
            Destroy(_rightHandTarget);
        }
        catch (Exception exc)
        {
            SuperController.LogError("Snug.OnDisable: " + exc);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }

    #endregion

    #region Debuggers

    private void CreateDebuggers()
    {
        DestroyDebuggers();

        var rightHandDbg = VisualCuesHelper.Cross(Color.red);
        var rightHand = SuperController.singleton.rightHand;
        rightHandDbg.transform.SetPositionAndRotation(
            rightHand.position,
            SuperController.singleton.rightHand.rotation
        );
        rightHandDbg.transform.parent = rightHand;
        _debuggers.Add(rightHandDbg);

        if (_rightHandController != null)
        {
            var rightHandControllerDbg = VisualCuesHelper.Cross(Color.blue);
            rightHandControllerDbg.transform.SetPositionAndRotation(
                _rightHandController.control.position,
                _rightHandController.control.rotation
            );
            rightHandControllerDbg.transform.parent = _rightHandController.control;
            _debuggers.Add(rightHandControllerDbg);
        }

        var rightWristDbg = VisualCuesHelper.Cross(Color.green);
        rightWristDbg.transform.SetPositionAndRotation(
            _rightHandTarget.transform.position,
            _rightHandTarget.transform.rotation
        );
        rightWristDbg.transform.parent = _rightHandTarget.transform;
        _debuggers.Add(rightWristDbg);

        foreach (var anchorPoint in _anchorPoints)
        {
            anchorPoint.VirtualCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.magenta);
            _debuggers.Add(anchorPoint.VirtualCue.gameObject);
            anchorPoint.Update();

            anchorPoint.PhysicalCue = new ControllerAnchorPointVisualCue(anchorPoint.RigidBody.transform, Color.yellow);
            _debuggers.Add(anchorPoint.PhysicalCue.gameObject);
            anchorPoint.Update();
        }
    }

    private void DestroyDebuggers()
    {
        foreach (var debugger in _debuggers)
        {
            Destroy(debugger);
        }
    }

    #endregion

    private class ControllerAnchorPoint
    {
        public string Label { get; internal set; }
        public Rigidbody RigidBody { get; set; }
        public Vector3 Offset { get; set; }
        public Vector2 Scale { get; set; }
        public Vector3 CueOffset { get; set; }
        public Vector2 CueScale { get; set; }
        public ControllerAnchorPointVisualCue VirtualCue { get; set; }
        public ControllerAnchorPointVisualCue PhysicalCue { get; set; }

        internal void Update()
        {
            if (PhysicalCue != null)
            {
                VirtualCue.Update(CueOffset, CueScale);
                PhysicalCue.Update(CueOffset + Offset, CueScale * Scale);
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
            _frontHandle.localScale = new Vector3(_width * 2, _width * 2, _width * 2);
            _frontHandle.transform.localPosition = Vector3.forward * size.y / 2;
            _leftHandle.localScale = new Vector3(_width * 2, _width * 2, _width * 2);
            _leftHandle.transform.localPosition = Vector3.left * size.x / 2;
            _rightHandle.localScale = new Vector3(_width * 2, _width * 2, _width * 2);
            _rightHandle.transform.localPosition = Vector3.right * size.x / 2;
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
            return go;
        }

        public static GameObject CreatePrimitive(Transform parent, PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color, renderQueue = 4000 };
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
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
