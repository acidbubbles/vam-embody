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
    private JSONStorableBool _activeJSON;
    private JSONStorableBool _rotationLockJSON;
    private JSONStorableVector3 _rotationOffsetJSON;
    private JSONStorableBool _positionLockJSON;
    private JSONStorableVector3 _positionOffsetJSON;
    private JSONStorableStringChooser _linkJSON;
    private Vector3 _previousPosition;
    private float _previousPlayerHeight;
    private bool _positionDirty;
    private Quaternion _previousRotation;
    private bool _rotationDirty;

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
        CreateSlider("Rotation Offset X", _rotationOffsetJSON.val.x, 180f, true, "F2")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float x)
            {
                _rotationOffsetJSON.val = new Vector3(x, _rotationOffsetJSON.val.y, _rotationOffsetJSON.val.z);
            }));
        CreateSlider("Rotation Offset Y", _rotationOffsetJSON.val.y, 180f, true, "F2")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float y)
            {
                _rotationOffsetJSON.val = new Vector3(_rotationOffsetJSON.val.x, y, _rotationOffsetJSON.val.z);
            }));
        CreateSlider("Rotation Offset Z", _rotationOffsetJSON.val.z, 180f, true, "F2")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float z)
            {
                _rotationOffsetJSON.val = new Vector3(_rotationOffsetJSON.val.x, _rotationOffsetJSON.val.y, z);
            }));

        _positionLockJSON = new JSONStorableBool("Position Lock", true);
        RegisterBool(_positionLockJSON);
        var positionLockToggle = CreateToggle(_positionLockJSON, true);

        _positionOffsetJSON = new JSONStorableVector3("Position", Vector3.zero, new Vector3(-2f, -2f, -2f), new Vector3(2f, 2f, 2f), false, true);
        RegisterVector3(_positionOffsetJSON);
        CreateSlider("Position Offset X", _positionOffsetJSON.val.x, 1f, false, "F4")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float x)
            {
                _positionOffsetJSON.val = new Vector3(x, _positionOffsetJSON.val.y, _positionOffsetJSON.val.z);
            }));
        CreateSlider("Position Offset Y", _positionOffsetJSON.val.y, 1f, false, "F4")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float y)
            {
                _positionOffsetJSON.val = new Vector3(_positionOffsetJSON.val.x, y, _positionOffsetJSON.val.z);
            }));
        CreateSlider("Position Offset Z", _positionOffsetJSON.val.z, 1f, false, "F4")
            .onValueChanged.AddListener(new UnityAction<float>(delegate (float z)
            {
                _positionOffsetJSON.val = new Vector3(_positionOffsetJSON.val.x, _positionOffsetJSON.val.y, z);
            }));
    }

    private Slider CreateSlider(string label, float val, float max, bool constrained, string format)
    {
        var uiElement = CreateUIElement(manager.configurableSliderPrefab.transform, true);
        var dynamicSlider = uiElement.GetComponent<UIDynamicSlider>();
        dynamicSlider.Configure(label, -max, max, val, constrained, format, true, !constrained);
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

            if (!_activeJSON.val || _link == null)
            {
                Restore();
                return;
            }

            var centerCameraTarget = superController.centerCameraTarget;
            var monitorCenterCamera = superController.MonitorCenterCamera;

            if (_rotationLockJSON.val)
            {
                if (!_rotationDirty)
                {
                    _previousRotation = navigationRig.rotation;
                    _rotationDirty = true;
                }
                var navigationRigRotation = _link.transform.rotation;
                navigationRigRotation *= Quaternion.Euler(_rotationOffsetJSON.val);
                navigationRig.rotation = navigationRigRotation;
            }

            if (_positionLockJSON.val)
            {
                if (!_positionDirty)
                {
                    _previousPosition = navigationRig.position;
                    _previousPlayerHeight = superController.playerHeightAdjust;
                    _positionDirty = true;
                }
                var up = navigationRig.up;
                var targetPosition = _link.position + _link.transform.forward * _positionOffsetJSON.val.z + _link.transform.right * _positionOffsetJSON.val.x + _link.transform.up * _positionOffsetJSON.val.y;
                var positionOffset = navigationRig.position + targetPosition - _possessor.autoSnapPoint.position;
                var playerHeightAdjustOffset = Vector3.Dot(positionOffset - navigationRig.position, up);
                navigationRig.position = positionOffset + up * -playerHeightAdjustOffset;
                superController.playerHeightAdjust += playerHeightAdjustOffset;
            }
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to update: " + e);
        }
    }

    public void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (_rotationDirty)
        {
            SuperController.singleton.navigationRig.rotation = _previousRotation;
            _rotationDirty = false;
        }
        if (_positionDirty)
        {
            SuperController.singleton.navigationRig.position = _previousPosition;
            SuperController.singleton.playerHeightAdjust = _previousPlayerHeight;
            _positionDirty = false;
        }
    }
}
