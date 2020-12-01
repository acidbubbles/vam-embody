using System;
using Interop;
using UnityEngine;

public class Embody : MVRScript
{
    private InteropProxy _interop;
    private JSONStorableStringChooser _toggleKeyJSON;
    private KeyCode _toggleKey = KeyCode.None;
    private FreeControllerV3 _headControl;
    private JSONStorableBool _passengerActiveJSON;
    private JSONStorableBool _useWorldScaleJSON;
    private JSONStorableBool _possessionActiveJSON;

    public override void Init()
    {
        _interop = new InteropProxy(this, containingAtom);
        _interop.StartInitDeferred();

        _headControl = (FreeControllerV3) containingAtom.GetStorableByID("headControl");

        _passengerActiveJSON = new JSONStorableBool("Passenger Active", false, val =>
        {
            if (!_interop.ready || _possessionActiveJSON.val) return;
            if (val)
            {
                if (_useWorldScaleJSON.val)
                    ((MVRScript) _interop.worldScale).enabledJSON.val = true;
                ((MVRScript) _interop.hideGeometry).enabledJSON.val = true;
                ((MVRScript) _interop.passenger).enabledJSON.val = true;
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
                ((MVRScript) _interop.worldScale).enabledJSON.val = true;
            ((MVRScript) _interop.hideGeometry).enabledJSON.val = true;
            ((MVRScript) _interop.cameraOffset).enabledJSON.val = true;
        })
        {
            isStorable = false
        };
        CreateToggle(_possessionActiveJSON).toggle.interactable = false;

        var keys = Enum.GetNames(typeof(KeyCode)).ToList();
        _toggleKeyJSON = new JSONStorableStringChooser("Toggle Key", keys, KeyCode.Space.ToString(), "Toggle Key", val => { _toggleKey = (KeyCode) Enum.Parse(typeof(KeyCode), val); });
        RegisterStringChooser(_toggleKeyJSON);
        var toggleKeyPopup = CreateFilterablePopup(_toggleKeyJSON);
        toggleKeyPopup.popupPanelHeight = 600f;

        _useWorldScaleJSON = new JSONStorableBool("UseWorldScale", true, (bool val) => Refresh());
        CreateToggle(_useWorldScaleJSON, true).label = "Use world scale";

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
    }

    public void Update()
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
        if (_interop.worldScale != null) ((MVRScript) _interop.worldScale).enabledJSON.val = false;
        if (_interop.cameraOffset != null) ((MVRScript) _interop.cameraOffset).enabledJSON.val = false;
        if (_interop.hideGeometry != null) ((MVRScript) _interop.hideGeometry).enabledJSON.val = false;
        if (_interop.passenger != null) ((MVRScript) _interop.passenger).enabledJSON.val = false;
    }
}
