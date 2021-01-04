using System;
using UnityEngine;

public interface IEmbody
{
    JSONStorableBool activeJSON { get; }
}

public class Embody : MVRScript, IEmbody
{
    public JSONStorableBool activeJSON { get; private set; }

    private JSONStorableBool _useWorldScaleJSON;
    private AutomationModule _automationModule;
    private HideGeometryModule _hideGeometryModule;
    private OffsetCameraModule _offsetCameraModule;
    private PassengerModule _passengerModule;
    private SnugModule _snugModule;
    private WorldScaleModule _worldScaleModule;
    private GameObject _modules;
    private ScreensManager _screensManager;

    public override void Init()
    {
        try
        {
            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            _automationModule = CreateModule<AutomationModule>();
            _hideGeometryModule = CreateModule<HideGeometryModule>();
            _offsetCameraModule = CreateModule<OffsetCameraModule>();
            _passengerModule = CreateModule<PassengerModule>();
            _snugModule = CreateModule<SnugModule>();
            _worldScaleModule = CreateModule<WorldScaleModule>();

            _automationModule.embody = this;
            _automationModule.Init();
            _hideGeometryModule.Init();
            _offsetCameraModule.Init();
            _passengerModule.Init();
            _snugModule.Init();
            _worldScaleModule.Init();

            _modules.SetActive(true);

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(this));
            _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(this, _passengerModule));
            _screensManager.Add(SnugSettingsScreen.ScreenName, new SnugSettingsScreen(this, _snugModule));
            _screensManager.Add(HideGeometrySettingsScreen.ScreenName, new HideGeometrySettingsScreen(this, _hideGeometryModule));
            _screensManager.Add(OffsetCameraSettingsScreen.ScreenName, new OffsetCameraSettingsScreen(this, _offsetCameraModule));
            _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(this));
            _screensManager.Add(PresetsScreen.ScreenName, new PresetsScreen(this));
            _screensManager.Add(AutomationSettingsScreen.ScreenName, new AutomationSettingsScreen(this, _automationModule));
            _screensManager.Show(MainScreen.ScreenName);
            CreateScrollablePopup(_screensManager.screensJSON).popupPanelHeight = 700f;

            // TODO: Choose a mode (Snug, Passenger or Standard), now only Passenger is here...
            activeJSON = new JSONStorableBool("Active", false, val =>
            {
                if (_automationModule.possessionActiveJSON.val)
                {
                    activeJSON.valNoCallback = false;
                    return;
                }

                if (val)
                {
                    // if (_useWorldScaleJSON.val)
                    //     _worldScaleModule.enabled = true;
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
            RegisterBool(activeJSON);
            CreateToggle(activeJSON, true);
        }
        catch (Exception)
        {
            enabledJSON.val = false;
            if (_modules != null) Destroy(_modules);
            throw;
        }
    }

    private T CreateModule<T>() where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = false;
        module.plugin = this;
        return module;
    }

    public void OnDisable()
    {
        activeJSON.val = false;
    }

    public void DeactivateAll()
    {
        if (_worldScaleModule != null) _worldScaleModule.enabled = false;
        if (_offsetCameraModule != null) _offsetCameraModule.enabled = false;
        if (_hideGeometryModule != null) _hideGeometryModule.enabled = false;
        if (_passengerModule != null) _passengerModule.enabled = false;
    }
}
