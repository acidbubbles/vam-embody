using System;
using UnityEngine;

public interface IEmbody
{
    JSONStorableBool activeJSON { get; }
}

public class Embody : MVRScript, IEmbody
{
    public JSONStorableBool activeJSON { get; private set; }

    private GameObject _modules;
    private ScreensManager _screensManager;

    public override void Init()
    {
        try
        {
            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            var automationModule = CreateModule<AutomationModule>();
            var hideGeometryModule = CreateModule<HideGeometryModule>();
            var offsetCameraModule = CreateModule<OffsetCameraModule>();
            var passengerModule = CreateModule<PassengerModule>();
            var snugModule = CreateModule<SnugModule>();
            var worldScaleModule = CreateModule<WorldScaleModule>();
            var eyeTargetModule = CreateModule<EyeTargetModule>();

            automationModule.embody = this;
            automationModule.Init();
            hideGeometryModule.Init();
            offsetCameraModule.Init();
            passengerModule.Init();
            snugModule.Init();
            worldScaleModule.Init();
            eyeTargetModule.Init();

            _modules.SetActive(true);

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(this));
            _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(this, passengerModule));
            _screensManager.Add(SnugSettingsScreen.ScreenName, new SnugSettingsScreen(this, snugModule));
            _screensManager.Add(HideGeometrySettingsScreen.ScreenName, new HideGeometrySettingsScreen(this, hideGeometryModule));
            _screensManager.Add(OffsetCameraSettingsScreen.ScreenName, new OffsetCameraSettingsScreen(this, offsetCameraModule));
            _screensManager.Add(AutomationSettingsScreen.ScreenName, new AutomationSettingsScreen(this, automationModule));
            _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(this));
            _screensManager.Add(EyeTargetSettingsScreen.ScreenName, new EyeTargetSettingsScreen(this, eyeTargetModule));
            _screensManager.Add(PresetsScreen.ScreenName, new PresetsScreen(this));

            _screensManager.Show(MainScreen.ScreenName);
            CreateScrollablePopup(_screensManager.screensJSON).popupPanelHeight = 700f;

            // TODO: Choose a mode (Snug, Passenger or Standard), now only Passenger is here...
            activeJSON = new JSONStorableBool("Active", false, val =>
            {
                if (automationModule.possessionActiveJSON.val)
                {
                    activeJSON.valNoCallback = false;
                    return;
                }

                if (val)
                {
                    // if (_useWorldScaleJSON.val)
                    //     _worldScaleModule.enabled = true;
                    hideGeometryModule.enabled = true;
                    passengerModule.enabled = true;
                }
                else
                {
                    if (worldScaleModule != null) worldScaleModule.enabled = false;
                    if (offsetCameraModule != null) offsetCameraModule.enabled = false;
                    if (hideGeometryModule != null) hideGeometryModule.enabled = false;
                    if (passengerModule != null) passengerModule.enabled = false;
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
}
