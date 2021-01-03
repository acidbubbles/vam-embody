﻿using System;
using UnityEngine;

public class Embody : MVRScript
{
    private JSONStorableStringChooser _toggleKeyJSON;
    private KeyCode _toggleKey = KeyCode.None;
    private FreeControllerV3 _headControl;
    private JSONStorableBool _passengerActiveJSON;
    private JSONStorableBool _useWorldScaleJSON;
    private JSONStorableBool _possessionActiveJSON;
    private HideGeometryModule _hideGeometryModule;
    private OffsetCameraModule _offsetCameraModule;
    private PassengerModule _passengerModule;
    private SnugModule _snugModule;
    private WorldScaleModule _worldScaleModule;
    private GameObject _modules;

    public override void Init()
    {
        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");

        _modules = new GameObject();
        _modules.transform.SetParent(transform, false);
        _modules.SetActive(false);

        _hideGeometryModule = CreateModule<HideGeometryModule>();
        _offsetCameraModule = CreateModule<OffsetCameraModule>();
        _passengerModule = CreateModule<PassengerModule>();
        _snugModule = CreateModule<SnugModule>();
        _worldScaleModule = CreateModule<WorldScaleModule>();

        _offsetCameraModule.Init();
        _hideGeometryModule.Init();
        _passengerModule.Init();
        _snugModule.Init();
        _worldScaleModule.Init();

        _modules.SetActive(true);

        _passengerActiveJSON = new JSONStorableBool("Passenger Active", false, val =>
        {
            if (_possessionActiveJSON.val)
            {
                _passengerActiveJSON.valNoCallback = false;
                return;
            }

            if (val)
            {
                if (_useWorldScaleJSON.val)
                    _worldScaleModule.enabled = true;
                _hideGeometryModule.enabled = true;
                _passengerModule.enabled = true;
            }
            else
            {
                DeactivateAll();
            }
        })
        {
            isStorable = false
        };
        RegisterBool(_passengerActiveJSON);
        CreateToggle(_passengerActiveJSON);

        _possessionActiveJSON = new JSONStorableBool("Possession Active (Auto)", false, (bool val) =>
        {
            if (_passengerActiveJSON.val)
                _passengerActiveJSON.val = false;

            if (_useWorldScaleJSON.val)
                _worldScaleModule.enabled = true;
            _hideGeometryModule.enabled = true;
            _offsetCameraModule.enabled = true;
        })
        {
            isStorable = false
        };
        CreateToggle(_possessionActiveJSON).toggle.interactable = false;

        var keys = Enum.GetNames(typeof(KeyCode)).ToList();
        keys.Remove(KeyCode.None.ToString());
        keys.Insert(0, KeyCode.None.ToString());
        _toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", keys, KeyCode.None.ToString(), "Toggle Key", val => { _toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        RegisterStringChooser(_toggleKeyJSON);
        var toggleKeyPopup = CreateFilterablePopup(_toggleKeyJSON);
        toggleKeyPopup.popupPanelHeight = 600f;

        _useWorldScaleJSON = new JSONStorableBool("UseWorldScale", true, (bool val) => Refresh());
        CreateToggle(_useWorldScaleJSON, true).label = "Use world scale";

        // TODO: Implement a dynamic UI
        /*
        var configureWorldScaleJSON = new JSONStorableAction("ConfigureWorldScale", () => { _interop.SelectPlugin(_interop.worldScale); });
        var configureWorldScaleBtn = CreateButton("Configure world scale", true);
        configureWorldScaleJSON.dynamicButton = configureWorldScaleBtn;

        var configureHideGeometryJSON = new JSONStorableAction("ConfigureHideGeometry", () => { _interop.SelectPlugin(_interop.hideGeometry); });
        var configureHideGeometryBtn = CreateButton("Configure hide geometry", true);
        configureHideGeometryJSON.dynamicButton = configureHideGeometryBtn;

        var configureCameraJSON = new JSONStorableAction("ConfigureCameraOffset", () => { _interop.SelectPlugin(_interop.cameraOffset); });
        var configureCameraBtn = CreateButton("Configure camera offset", true);
        configureCameraJSON.dynamicButton = configureCameraBtn;

        var configurePassengerJSON = new JSONStorableAction("ConfigurePassenger", () => { _interop.SelectPlugin(_interop.passenger); });
        var configurePassengerBtn = CreateButton("Configure Passenger", true);
        configurePassengerJSON.dynamicButton = configurePassengerBtn;

        var configureSnugJSON = new JSONStorableAction("ConfigureSnug", () => { _interop.SelectPlugin(_interop.snug); });
        var configureSnugBtn = CreateButton("Configure Snug", true);
        configureSnugJSON.dynamicButton = configureSnugBtn;
        */
    }

    private T CreateModule<T>() where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = false;
        module.plugin = this;
        return module;
    }

    public void Update()
    {
        if (_headControl != null)
        {
            if (_headControl.possessed)
            {
                _possessionActiveJSON.val = true;
                return;
            }

            if (_possessionActiveJSON.val && !_headControl.possessed)
            {
                _possessionActiveJSON.val = false;
                return;
            }
        }

        if (!_passengerActiveJSON.val)
        {
            if (!LookInputModule.singleton.inputFieldActive && _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
                _passengerActiveJSON.val = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || _toggleKey != KeyCode.None && Input.GetKeyDown(_toggleKey))
        {
                _passengerActiveJSON.val = false;
        }
    }

    public void OnDisable()
    {
        _passengerActiveJSON.val = false;
    }

    public void Refresh()
    {
        if (!_passengerActiveJSON.val) return;
        _passengerActiveJSON.val = false;
        _passengerActiveJSON.val = true;
    }

    public void DeactivateAll()
    {
        if (_worldScaleModule != null) _worldScaleModule.enabled = false;
        if (_offsetCameraModule != null) _offsetCameraModule.enabled = false;
        if (_hideGeometryModule != null) _hideGeometryModule.enabled = false;
        if (_passengerModule != null) _passengerModule.enabled = false;
    }
}
