#define VAM_DIAGNOSTICS
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Passenger Version 0.0.0
/// Your eyes follow the model's eyes, not the other way around
/// </summary>
public class Passenger : MVRScript
{
    private Rigidbody _link;
    private Possessor _possessor;
    private JSONStorableStringChooser _linkJSON;
    private JSONStorableBool _activeJSON;
    private JSONStorableVector3 _rotationOffsetJSON;
    private JSONStorableVector3 _positionOffsetJSON;
    private JSONStorableBool _rotationLockJSON;
    private JSONStorableBool _positionLockJSON;

    public override void Init()
    {
        try
        {
            _possessor = SuperController.singleton.centerCameraTarget.transform.GetComponent<Possessor>();

            InitControls();
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize plugin: " + e);
        }
    }

    private void InitControls()
    {
        var links = containingAtom.linkableRigidbodies.Select(c => c.name).ToList();

        var defaultLink = containingAtom.type == "Person" ? "head" : "object";
        _linkJSON = new JSONStorableStringChooser("Target Controller", links, defaultLink, "Target Controller: ", OnLinkChanged);
        _linkJSON.storeType = JSONStorableParam.StoreType.Physical;
        RegisterStringChooser(_linkJSON);

        var linkPopup = CreateScrollablePopup(_linkJSON, false);
        linkPopup.popupPanelHeight = 600f;

        if (!string.IsNullOrEmpty(_linkJSON.val))
            OnLinkChanged(_linkJSON.val);

        _activeJSON = new JSONStorableBool("Active", false);
        RegisterBool(_activeJSON);
        var activeToggle = CreateToggle(_activeJSON, false);

        _rotationLockJSON = new JSONStorableBool("Rotation Lock", false);
        RegisterBool(_rotationLockJSON);
        var rotationLockToggle = CreateToggle(_rotationLockJSON, true);

        _rotationOffsetJSON = new JSONStorableVector3("Rotate", Vector3.zero, new Vector3(-180, -180, -180), new Vector3(180, 180, 180), true, true);
        RegisterVector3(_rotationOffsetJSON);
        CreateSlider("Rotation Offset X", _rotationOffsetJSON.val.x, 180f, true).onValueChanged.AddListener(new UnityAction<float>(delegate (float x)
        {
            _rotationOffsetJSON.val = new Vector3(x, _rotationOffsetJSON.val.y, _rotationOffsetJSON.val.z);
        }));
        CreateSlider("Rotation Offset Y", _rotationOffsetJSON.val.y, 180f, true).onValueChanged.AddListener(new UnityAction<float>(delegate (float y)
        {
            _rotationOffsetJSON.val = new Vector3(_rotationOffsetJSON.val.x, y, _rotationOffsetJSON.val.z);
        }));
        CreateSlider("Rotation Offset Z", _rotationOffsetJSON.val.z, 180f, true).onValueChanged.AddListener(new UnityAction<float>(delegate (float z)
        {
            _rotationOffsetJSON.val = new Vector3(_rotationOffsetJSON.val.x, _rotationOffsetJSON.val.y, z);
        }));

        _positionLockJSON = new JSONStorableBool("Position Lock", true);
        RegisterBool(_positionLockJSON);
        var positionLockToggle = CreateToggle(_positionLockJSON, true);

        _positionOffsetJSON = new JSONStorableVector3("Position", Vector3.zero, new Vector3(-2f, -2f, -2f), new Vector3(2f, 2f, 2f), false, true);
        RegisterVector3(_positionOffsetJSON);
        CreateSlider("Position Offset X", _positionOffsetJSON.val.x, 2f, false).onValueChanged.AddListener(new UnityAction<float>(delegate (float x)
        {
            _positionOffsetJSON.val = new Vector3(x, _positionOffsetJSON.val.y, _positionOffsetJSON.val.z);
        }));
        CreateSlider("Position Offset Y", _positionOffsetJSON.val.y, 2f, false).onValueChanged.AddListener(new UnityAction<float>(delegate (float y)
        {
            _positionOffsetJSON.val = new Vector3(_positionOffsetJSON.val.x, y, _positionOffsetJSON.val.z);
        }));
        CreateSlider("Position Offset Z", _positionOffsetJSON.val.z, 2f, false).onValueChanged.AddListener(new UnityAction<float>(delegate (float z)
        {
            _positionOffsetJSON.val = new Vector3(_positionOffsetJSON.val.x, _positionOffsetJSON.val.y, z);
        }));

    }

    private Slider CreateSlider(string label, float val, float max, bool constrained)
    {
        var uiElement = CreateUIElement(manager.configurableSliderPrefab.transform, true);
        var dynamicSlider = uiElement.GetComponent<UIDynamicSlider>();
        dynamicSlider.Configure(label, -max, max, val, constrained, "F2", true, !constrained);
        return dynamicSlider.slider;
    }

    private void DebugValue(float debugValue)
    {
        SuperController.singleton.ClearMessages();
        SuperController.LogMessage("New value: " + debugValue);
    }

    private void OnLinkChanged(string linkName)
    {
        if (containingAtom == null) throw new NullReferenceException("containingAtom");

        _link = containingAtom.linkableRigidbodies.FirstOrDefault(c => c.name == linkName);
        if (_link == null)
            SuperController.LogError("Controller does not exist: " + linkName);
    }

    public void Update()
    {
        try
        {
            var superController = SuperController.singleton;
            var navigationRig = superController.navigationRig;

            if (!_activeJSON.val || _link == null){
                // TODO: Reset angle to a sane value
                /*
                Vector3 rigAngles = navigationRig.localEulerAngles;
                if (rigAngles.z != 0f){
                navigationRig.localEulerAngles = new Vector3(rigAngles.x, rigAngles.y, 0f);
                }
                */
                return;
            }

            var centerCameraTarget = superController.centerCameraTarget;
            var monitorCenterCamera = superController.MonitorCenterCamera;

            var up = navigationRig.up;
            var forward = navigationRig.forward;
            var right = navigationRig.right;

            if (_rotationLockJSON.val)
            {
                var navigationRigRotation = _link.transform.rotation;
                navigationRigRotation *= Quaternion.Euler(_rotationOffsetJSON.val);
                navigationRig.rotation = navigationRigRotation;
            }

            if (_positionLockJSON.val)
            {
                var positionOffset = navigationRig.position + _link.position - _possessor.autoSnapPoint.position;
                positionOffset += forward * _positionOffsetJSON.val.z + right * _positionOffsetJSON.val.x;
                var playerHeightAdjustOffset = Vector3.Dot(positionOffset - navigationRig.position, up);
                navigationRig.position = positionOffset + up * -playerHeightAdjustOffset;
                superController.playerHeightAdjust += playerHeightAdjustOffset + _positionOffsetJSON.val.y;
            }
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update: " + e);
        }
    }
}
