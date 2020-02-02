using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Wrist
/// By Acidbubbles
/// Better controller alignement when possessing
/// Source: https://github.com/acidbubbles/vam-wrist
/// </summary>
public class Wrist : MVRScript
{
    private readonly List<GameObject> _debuggers = new List<GameObject>();
    private readonly List<ControllerOffsetPoint> _offsetPoints = new List<ControllerOffsetPoint>();
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
                SuperController.LogError("Wrist can only be applied on Person atoms.");
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
                SuperController.LogError("Wrist can only be applied in VR");
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
                    SuperController.LogError("Wrist: Cannot activate while possessing");
                    return;
                }
                if (val)
                {
                    _rightHandController.PossessMoveAndAlignTo(_rightHandTarget.transform);
                    _rightHandController.SelectLinkToRigidbody(_rightHandTarget.GetComponent<Rigidbody>());
                }
                else
                {
                    _rightHandController.RestorePreLinkState();
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

            var rigidBodiesJSON = new JSONStorableStringChooser("Target", containingAtom.rigidbodies.Select(rb => rb.name).OrderBy(n => n).ToList(), "", "Target", (string val) => DisplayRigidBody(val))
            {
                isStorable = false
            };
            CreateScrollablePopup(rigidBodiesJSON);

            _offsetPoints.Add(new ControllerOffsetPoint
            {
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger"),
                Offset = new Vector3(0, 0, 0),
                Scale = new Vector3(1, 0, 1)
            });
            _offsetPoints.Add(new ControllerOffsetPoint
            {
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "chest"),
                Offset = new Vector3(0, 0, 0),
                Scale = new Vector3(1, 0, 1)
            });
            _offsetPoints.Add(new ControllerOffsetPoint
            {
                RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "abdomen"),
                Offset = new Vector3(0, 0, 0),
                Scale = new Vector3(1, 0, 1)
            });

            _wristOffsetXJSON = new JSONStorableFloat("Right Wrist Offset X", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetXJSON);
            CreateSlider(_wristOffsetXJSON, true);
            _wristOffsetYJSON = new JSONStorableFloat("Right Wrist Offset Y", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetYJSON);
            CreateSlider(_wristOffsetYJSON, true);
            _wristOffsetZJSON = new JSONStorableFloat("Right Wrist Offset Z", -0.01f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetZJSON);
            CreateSlider(_wristOffsetZJSON, true);

            _wristRotateXJSON = new JSONStorableFloat("Right Wrist Rotate X", 8.33f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateXJSON);
            CreateSlider(_wristRotateXJSON, true);
            _wristRotateYJSON = new JSONStorableFloat("Right Wrist Rotate Y", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateYJSON);
            CreateSlider(_wristRotateYJSON, true);
            _wristRotateZJSON = new JSONStorableFloat("Right Wrist Rotate Z", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateZJSON);
            CreateSlider(_wristRotateZJSON, true);
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.Init: " + exc);
        }

        StartCoroutine(DeferredInit());
    }

    private GameObject _rbDebugger;
    private void DisplayRigidBody(string val)
    {
        if (_rbDebugger != null)
        {
            Destroy(_rbDebugger);
            _debuggers.Remove(_rbDebugger);
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
        _rbDebugger = debugger;
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
            SuperController.LogError("Wrist.DeferredInit: " + exc);
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
            SuperController.LogError("Wrist.Update: " + exc);
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

            ControllerOffsetPoint lower = null;
            ControllerOffsetPoint upper = null;
            foreach (var offsetPoint in _offsetPoints)
            {
                var offsetPointY = offsetPoint.RigidBody.transform.position.y;
                if (position.y > offsetPointY && (lower == null || offsetPointY > lower.RigidBody.transform.position.y))
                {
                    lower = offsetPoint;
                }
                else if (position.y < offsetPointY && (upper == null || offsetPointY < upper.RigidBody.transform.position.y))
                {
                    upper = offsetPoint;
                }
            }

            SuperController.singleton.ClearMessages();
            SuperController.LogMessage($"Between {lower?.RigidBody.name ?? "(none)"} and {upper?.RigidBody.name ?? "(none)"}");

            _rightHandTarget.transform.SetPositionAndRotation(
                position + snapOffset + _palmToWristOffset,
                _rightAutoSnapPoint.rotation * _rotateOffset
            );
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.FixedUpdate: " + exc);
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
            SuperController.LogError("Wrist.OnEnable: " + exc);
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
            SuperController.LogError("Wrist.OnDisable: " + exc);
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

        foreach (var offsetPoint in _offsetPoints)
        {
            var offsetPointDebugger = CreateDebugger(Color.yellow);
            offsetPointDebugger.transform.SetPositionAndRotation(
                offsetPoint.RigidBody.transform.position,
                offsetPoint.RigidBody.transform.rotation
            );
            offsetPointDebugger.transform.parent = offsetPoint.RigidBody.transform;
            _debuggers.Add(offsetPointDebugger);
        }
    }

    private GameObject CreateDebugger(Color color)
    {
        var go = new GameObject($"{name}.debugger.{Guid.NewGuid()}");
        CreateDebuggerLine(color, new Vector3(0.01f, 0.01f, 0.15f)).transform.parent = go.transform;
        CreateDebuggerLine(color, new Vector3(0.01f, 0.15f, 0.01f)).transform.parent = go.transform;
        CreateDebuggerLine(color, new Vector3(0.15f, 0.01f, 0.01f)).transform.parent = go.transform;
        return go;
    }

    private static GameObject CreateDebuggerLine(Color color, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().material.color = color;
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

    private class ControllerOffsetPoint
    {
        public Rigidbody RigidBody { get; set; }
        public Vector3 Offset { get; set; }
        public Vector3 Scale { get; set; }
    }
}
