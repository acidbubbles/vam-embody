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
    private JSONStorableBool _debugJSON;

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
            Transform touchObjectRight;
            if (s.isOVR)
                touchObjectRight = s.touchObjectRight;
            else if (s.isOpenVR)
                touchObjectRight = s.viveObjectRight;
            else
            {
                SuperController.LogError("Snug can only be applied in VR");
                return;
            }
            _rightAutoSnapPoint = touchObjectRight.GetComponent<Possessor>().autoSnapPoint;

            _rightHandController = containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
            if (_rightHandController == null) throw new NullReferenceException("Could not find the rHandControl controller");

            _rightHandTarget = new GameObject($"{containingAtom.gameObject.name}.lHandController.vamWristTarget");
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

            _debugJSON = new JSONStorableBool("Debug", false, (bool val) =>
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
            CreateToggle(_debugJSON);

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
                        var debugger = CreateDebugger(Color.white);
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
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
                Offset = new Vector3(0, 0.01f, -0.05f),
                Scale = new Vector3(1, 0, 1)
            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
                Offset = new Vector3(0, 0, 0.08f),
                Scale = new Vector3(1, 0, 1)
            });
            _anchorPoints.Add(new ControllerAnchorPoint
            {
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
                Offset = new Vector3(0, -0.05f, 0.1f),
                Scale = new Vector3(1, 0, 1)
            });

            // TODO: Figure out a simple way to configure this quickly (e.g. two moveable controllers, one for the vr body, the other for the real space equivalent)

            _wristOffsetXJSON = new JSONStorableFloat("Right Snug Offset X", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetXJSON);
            CreateSlider(_wristOffsetXJSON, true);
            _wristOffsetYJSON = new JSONStorableFloat("Right Snug Offset Y", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetYJSON);
            CreateSlider(_wristOffsetYJSON, true);
            _wristOffsetZJSON = new JSONStorableFloat("Right Snug Offset Z", -0.01f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetZJSON);
            CreateSlider(_wristOffsetZJSON, true);

            _wristRotateXJSON = new JSONStorableFloat("Right Snug Rotate X", 8.33f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateXJSON);
            CreateSlider(_wristRotateXJSON, true);
            _wristRotateYJSON = new JSONStorableFloat("Right Snug Rotate Y", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateYJSON);
            CreateSlider(_wristRotateYJSON, true);
            _wristRotateZJSON = new JSONStorableFloat("Right Snug Rotate Z", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateZJSON);
            CreateSlider(_wristRotateZJSON, true);
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
        if (!_ready) return;
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

        var rightHandDbg = CreateDebugger(Color.red);
        var rightHand = SuperController.singleton.rightHand;
        rightHandDbg.transform.SetPositionAndRotation(
            rightHand.position,
            SuperController.singleton.rightHand.rotation
        );
        rightHandDbg.transform.parent = rightHand;
        _debuggers.Add(rightHandDbg);

        if (_rightHandController != null)
        {
            var rightHandControllerDbg = CreateDebugger(Color.blue);
            rightHandControllerDbg.transform.SetPositionAndRotation(
                _rightHandController.control.position,
                _rightHandController.control.rotation
            );
            rightHandControllerDbg.transform.parent = _rightHandController.control;
            _debuggers.Add(rightHandControllerDbg);
        }

        var rightWristDbg = CreateDebugger(Color.green);
        rightWristDbg.transform.SetPositionAndRotation(
            _rightHandTarget.transform.position,
            _rightHandTarget.transform.rotation
        );
        rightWristDbg.transform.parent = _rightHandTarget.transform;
        _debuggers.Add(rightWristDbg);

        foreach (var anchorPoint in _anchorPoints)
        {
            var anchorPointDebugger = CreateDebugger(Color.yellow);
            anchorPointDebugger.transform.SetPositionAndRotation(
                anchorPoint.RigidBody.transform.position,
                anchorPoint.RigidBody.transform.rotation
            );
            anchorPointDebugger.transform.parent = anchorPoint.RigidBody.transform;
            _debuggers.Add(anchorPointDebugger);

            var anchorOffsetDebugger = CreateDebugger(Color.grey);
            anchorOffsetDebugger.transform.SetPositionAndRotation(
                anchorPoint.RigidBody.transform.position + (anchorPoint.RigidBody.transform.rotation * anchorPoint.Offset),
                anchorPoint.RigidBody.transform.rotation
            );
            anchorOffsetDebugger.transform.parent = anchorPoint.RigidBody.transform;
            _debuggers.Add(anchorOffsetDebugger);
        }
    }

    private GameObject CreateDebugger(Color color)
    {
        var go = new GameObject($"{name}.debugger.{Guid.NewGuid()}");
        CreateDebuggerPrimitive(PrimitiveType.Cube, Color.red, new Vector3(0.200f, 0.005f, 0.005f)).transform.parent = go.transform;
        CreateDebuggerPrimitive(PrimitiveType.Cube, Color.green, new Vector3(0.005f, 0.200f, 0.005f)).transform.parent = go.transform;
        CreateDebuggerPrimitive(PrimitiveType.Cube, Color.blue, new Vector3(0.005f, 0.005f, 0.200f)).transform.parent = go.transform;
        CreateDebuggerPrimitive(PrimitiveType.Sphere, color, new Vector3(0.03f, 0.03f, 0.03f)).transform.parent = go.transform;
        return go;
    }

    private static GameObject CreateDebuggerPrimitive(PrimitiveType type, Color color, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.localScale = localScale;
        var material = go.GetComponent<Renderer>().material;
        material.color = color;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        return go;
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
        public Rigidbody RigidBody { get; set; }
        public Vector3 Offset { get; set; }
        public Vector3 Scale { get; set; }
    }
}
