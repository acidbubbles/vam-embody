using System;
using System.Collections;
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
    private Vector3 _offset;
    private Quaternion _rotate;
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
            var rb = _rightHandTarget.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.detectCollisions = false;

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

            _wristOffsetXJSON = new JSONStorableFloat("Right Wrist Offset X", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetXJSON);
            CreateSlider(_wristOffsetXJSON);
            _wristOffsetYJSON = new JSONStorableFloat("Right Wrist Offset Y", 0f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetYJSON);
            CreateSlider(_wristOffsetYJSON);
            _wristOffsetZJSON = new JSONStorableFloat("Right Wrist Offset Z", -0.01f, UpdateRightHandOffset, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetZJSON);
            CreateSlider(_wristOffsetZJSON);

            _wristRotateXJSON = new JSONStorableFloat("Right Wrist Rotate X", 8.33f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateXJSON);
            CreateSlider(_wristRotateXJSON);
            _wristRotateYJSON = new JSONStorableFloat("Right Wrist Rotate Y", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateYJSON);
            CreateSlider(_wristRotateYJSON);
            _wristRotateZJSON = new JSONStorableFloat("Right Wrist Rotate Z", 0f, UpdateRightHandOffset, -25f, 25f, true);
            RegisterFloat(_wristRotateZJSON);
            CreateSlider(_wristRotateZJSON);
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
        _offset = new Vector3(_wristOffsetXJSON.val, _wristOffsetYJSON.val, _wristOffsetZJSON.val);
        _rotate = Quaternion.Euler(_wristRotateXJSON.val, _wristRotateYJSON.val, _wristRotateZJSON.val);
    }

    #region Update

    public void Update()
    {
        if (!_ready) return;
        try
        {
            if (_rightHandDebugger != null)
            {
                var rightHand = SuperController.singleton.rightHand;
                _rightHandDebugger.transform.SetPositionAndRotation(
                    rightHand.position,
                    SuperController.singleton.rightHand.rotation
                );
                _rightHandDebugger.transform.Rotate(Vector3.right, 90);
            }

            if (_rightHandControllerDebugger != null && _rightHandController != null)
            {
                _rightHandControllerDebugger.transform.SetPositionAndRotation(
                    _rightHandController.control.position,
                    _rightHandController.control.rotation
                );
                _rightHandControllerDebugger.transform.Rotate(Vector3.right, 90);
            }

            if (_rightWristDebugger != null)
            {
                _rightWristDebugger.transform.SetPositionAndRotation(
                    _rightHandTarget.transform.position,
                    _rightHandTarget.transform.rotation
                );
                _rightWristDebugger.transform.Rotate(Vector3.right, 90);
            }
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.Update: " + exc);
            OnDisable();
        }
    }

    public void FixedUpdate()
    {
        if (!_ready) return;
        try
        {
            _rightHandTarget.transform.SetPositionAndRotation(
                _rightAutoSnapPoint.position + _offset,
                _rightAutoSnapPoint.rotation * _rotate
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
            CreateDebuggers();
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
        _rightWristDebugger = CreateDebugger(Color.green);
        _rightHandControllerDebugger = CreateDebugger(Color.blue);
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
    }

    #endregion
}
