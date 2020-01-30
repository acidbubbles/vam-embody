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
    private bool _ready;

    public override void Init()
    {
        try
        {
            if (containingAtom.type != "Person")
            {
                SuperController.LogError("Wrist can only be applied on Person atoms.");
                return;
            }

            _rightHandController = containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
            if (_rightHandController == null) throw new NullReferenceException("Could not find the rHandControl controller");

            _possessRightHandJSON = new JSONStorableBool("Possess Right Hand", false);
            RegisterBool(_possessRightHandJSON);
            CreateToggle(_possessRightHandJSON);

            _wristOffsetXJSON = new JSONStorableFloat("Right Wrist Offset X", 0f, UpdatePossessPoint, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetXJSON);
            CreateSlider(_wristOffsetXJSON);
            _wristOffsetYJSON = new JSONStorableFloat("Right Wrist Offset Y", 0f, UpdatePossessPoint, -0.2f, 0.2f, true);
            RegisterFloat(_wristOffsetYJSON);
            CreateSlider(_wristOffsetYJSON);
            _wristOffsetZJSON = new JSONStorableFloat("Right Wrist Offset Z", -0.1f, UpdatePossessPoint, -0.2f, 0f, true);
            RegisterFloat(_wristOffsetZJSON);
            CreateSlider(_wristOffsetZJSON);

            _wristRotateXJSON = new JSONStorableFloat("Right Wrist Rotate X", 0f, UpdatePossessPoint, -10f, 10f, true);
            RegisterFloat(_wristRotateXJSON);
            CreateSlider(_wristRotateXJSON);
            _wristRotateYJSON = new JSONStorableFloat("Right Wrist Rotate Y", 0f, UpdatePossessPoint, -10f, 10f, true);
            RegisterFloat(_wristRotateYJSON);
            CreateSlider(_wristRotateYJSON);
            _wristRotateZJSON = new JSONStorableFloat("Right Wrist Rotate Z", 0f, UpdatePossessPoint, -10f, 10f, true);
            RegisterFloat(_wristRotateZJSON);
            CreateSlider(_wristRotateZJSON);

            if (_rightHandController.possessPoint != null) SuperController.LogError("Seems like Virt-A-Mate now provides a custom possess point for hands! This plugin is probably obsolete, and will overwrite the native possess point.");
            _rightHandController.possessPoint = new GameObject($"{containingAtom.gameObject.name}.wristPlugin.rHandControlPossessPoint").transform;
            _rightHandController.possessPoint.parent = _rightHandController.control;
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
            UpdatePossessPoint(0);
            OnEnable();
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.DeferredInit: " + exc);
        }
    }

    private void UpdatePossessPoint(float _)
    {
        if (_rightHandController?.possessPoint == null) return;
        _rightHandController.possessPoint.transform.SetPositionAndRotation(
            _rightHandController.control.position + new Vector3(_wristOffsetXJSON.val, _wristOffsetYJSON.val, _wristOffsetZJSON.val),
            _rightHandController.control.rotation * Quaternion.Euler(_wristRotateXJSON.val, _wristRotateYJSON.val, _wristRotateZJSON.val)
        );

        // Refresh
        // TODO: Check what the controller allows
        _rightHandController.PossessMoveAndAlignTo(motionControllerRight.GetComponent<Possessor>().autoSnapPoint);
        _rightHandController.SelectLinkToRigidbody(motionControllerRight.GetComponent<Rigidbody>(), FreeControllerV3.SelectLinkState.PositionAndRotation);
    }

    // NOTE: This is coming directly from VaM (don't know why this is private)
    private Transform motionControllerRight
    {
        get
        {
            var s = SuperController.singleton;
            if (s.isOVR)
            {
                return s.touchObjectRight;
            }
            if (s.isOpenVR)
            {
                return s.viveObjectRight;
            }
            return null;
        }
    }

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

            if (_rightWristDebugger != null && _rightHandController?.possessPoint != null)
            {
                var possessPoint = _rightHandController.possessPoint;
                _rightWristDebugger.transform.SetPositionAndRotation(
                    possessPoint.position,
                    possessPoint.rotation
                );
                _rightWristDebugger.transform.Rotate(Vector3.right, 90);
            }
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.Update: " + exc);
        }
    }

    public void OnEnable()
    {
        try
        {
            _rightHandDebugger = CreateDebugger(Color.red);
            _rightWristDebugger = CreateDebugger(Color.green);
            _rightHandControllerDebugger = CreateDebugger(Color.blue);
            _ready = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.OnEnable: " + exc);
        }
    }

    private GameObject CreateDebugger(Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        go.GetComponent<Renderer>().material.color = color;
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Destroy(c);
        SuperController.LogMessage(string.Join(", ", go.GetComponentsInChildren<MonoBehaviour>().Select(c => c.name + ":" + c.ToString()).ToArray()));
        return go;
    }

    public void OnDisable()
    {
        try
        {
            Destroy(_rightHandDebugger);
            Destroy(_rightWristDebugger);
            Destroy(_rightHandControllerDebugger);
            Destroy(_rightHandController.possessPoint);
            _rightHandController.possessPoint = null;
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
}
