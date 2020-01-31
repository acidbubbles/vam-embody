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
    private GameObject _rightHandTarget;
    private JSONStorableBool _possessRightHandJSON;
    public JSONStorableFloat _wristOffsetXJSON;
    private JSONStorableFloat _wristOffsetYJSON;
    private JSONStorableFloat _wristOffsetZJSON;
    public JSONStorableFloat _wristRotateXJSON;
    private JSONStorableFloat _wristRotateYJSON;
    private JSONStorableFloat _wristRotateZJSON;
    private GameObject _rightHandDebugger;
    private GameObject _rightWristDebugger;
    private GameObject _rightHandControllerDebugger;
    private FreeControllerV3 _rightHandController;
    private Transform _rightAutoSnapPoint;
    private bool _ready;
    private Vector3 _palmToWristOffset;
    private Quaternion _rotateOffset;
    private JSONStorableBool _debugJSON;

    private List<ControllerOffsetPoint> _offsetPoints;

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

            var rigidBodiesJSON = new JSONStorableStringChooser("Target", containingAtom.rigidbodies.Select(rb => rb.name).ToList(), "", "Target")
            {
                isStorable = false
            };
            CreateScrollablePopup(rigidBodiesJSON);

            _offsetPoints = new List<ControllerOffsetPoint>
            {
                new ControllerOffsetPoint
                {
                    RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "lNipple"),
                    Weight = 0.8f
                },
                new ControllerOffsetPoint
                {
                    RigidBody = containingAtom.rigidbodies.First(rb => rb.name == "rNipple"),
                    Weight = 0.8f
                }
            };

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

            var snapDistance = 0.3f;
            var closePoints = new List<WeightedVector3>();
            foreach (var offsetPoint in _offsetPoints)
            {
                var distance = Vector3.Distance(position, offsetPoint.RigidBody.transform.position);
                if (distance > snapDistance) continue;
                closePoints.Add(new WeightedVector3
                {
                    weight = (1f - (distance / snapDistance)) * offsetPoint.Weight,
                    vector = offsetPoint.RigidBody.transform.position
                });
            }

            SuperController.singleton.ClearMessages();
            if (closePoints.Count > 0)
            {
                foreach (var v in closePoints)
                {
                    SuperController.LogMessage($"  - From {position} to {v.vector} weight {v.weight}");
                    position += (v.vector - position) * v.weight;
                }
            }
            else
            {
                SuperController.LogMessage("No close points");
            }

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

        _rightHandDebugger = CreateDebugger(Color.red);
        var rightHand = SuperController.singleton.rightHand;
        _rightHandDebugger.transform.SetPositionAndRotation(
            rightHand.position,
            SuperController.singleton.rightHand.rotation
        );
        _rightHandDebugger.transform.Rotate(Vector3.right, 90);
        _rightHandDebugger.transform.parent = rightHand;

        if (_rightHandController != null)
        {
            _rightHandControllerDebugger = CreateDebugger(Color.blue);
            _rightHandControllerDebugger.transform.SetPositionAndRotation(
                _rightHandController.control.position,
                _rightHandController.control.rotation
            );
            _rightHandControllerDebugger.transform.Rotate(Vector3.right, 90);
            _rightHandControllerDebugger.transform.parent = _rightHandController.control;
        }

        _rightWristDebugger = CreateDebugger(Color.green);
        _rightWristDebugger.transform.SetPositionAndRotation(
            _rightHandTarget.transform.position,
            _rightHandTarget.transform.rotation
        );
        _rightWristDebugger.transform.Rotate(Vector3.right, 90);
        _rightWristDebugger.transform.parent = _rightHandTarget.transform;

        foreach (var offsetPoint in _offsetPoints)
        {
            offsetPoint.Debugger = CreateDebugger(Color.magenta);
            offsetPoint.Debugger.transform.SetPositionAndRotation(offsetPoint.RigidBody.transform.position, offsetPoint.RigidBody.transform.rotation);
            offsetPoint.Debugger.transform.parent = offsetPoint.RigidBody.transform;
        }
    }

    private GameObject CreateDebugger(Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(0.03f, 0.03f, 0.05f);
        go.GetComponent<Renderer>().material.color = color;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        return go;
    }

    private void DestroyDebuggers()
    {
        Destroy(_rightHandDebugger);
        Destroy(_rightWristDebugger);
        Destroy(_rightHandControllerDebugger);
        foreach (var offsetPoint in _offsetPoints)
        {
            Destroy(offsetPoint.Debugger);
            offsetPoint.Debugger = null;
        }
    }

    #endregion

    private class ControllerOffsetPoint
    {
        public Rigidbody RigidBody { get; set; }
        public float Weight { get; internal set; }
        public GameObject Debugger { get; set; }
    }
}
