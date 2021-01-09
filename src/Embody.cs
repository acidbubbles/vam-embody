using System;
using System.Collections;
using SimpleJSON;
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
    private bool _restored;

    public override void Init()
    {
        try
        {
            activeJSON = new JSONStorableBool("Active", false) {isStorable = false};

            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            // TODO: Filter based on whether it's a person atom

            var automationModule = CreateModule<AutomationModule>();
            automationModule.embody = this;
            var trackersModule = CreateModule<TrackersModule>();
            var hideGeometryModule = CreateModule<HideGeometryModule>();
            var offsetCameraModule = CreateModule<OffsetCameraModule>();
            var passengerModule = CreateModule<PassengerModule>();
            var snugModule = CreateModule<SnugModule>();
            snugModule.trackers = trackersModule;
            var worldScaleModule = CreateModule<WorldScaleModule>();
            var eyeTargetModule = CreateModule<EyeTargetModule>();

            // TODO: Once awaken, register the useful storables so they can be modified by scripts

            _modules.SetActive(true);

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(this, _modules.GetComponents<IEmbodyModule>()));
            _screensManager.Add(TrackersSettingsScreen.ScreenName, new TrackersSettingsScreen(this, trackersModule));
            _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(this, passengerModule));
            _screensManager.Add(SnugSettingsScreen.ScreenName, new SnugSettingsScreen(this, snugModule));
            _screensManager.Add(HideGeometrySettingsScreen.ScreenName, new HideGeometrySettingsScreen(this, hideGeometryModule));
            _screensManager.Add(OffsetCameraSettingsScreen.ScreenName, new OffsetCameraSettingsScreen(this, offsetCameraModule));
            _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(this, worldScaleModule));
            _screensManager.Add(EyeTargetSettingsScreen.ScreenName, new EyeTargetSettingsScreen(this, eyeTargetModule));
            _screensManager.Add(AutomationSettingsScreen.ScreenName, new AutomationSettingsScreen(this, automationModule));
            _screensManager.Add(ImportExportScreen.ScreenName, new ImportExportScreen(this, this));

            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                {
                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        module.enabledJSON.val = module.selectedJSON.val;
                    }
                }
                else
                {
                    automationModule.Reset();
                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        module.enabledJSON.val = false;
                    }
                }
            };
            RegisterBool(activeJSON);

            var activeToggle = CreateToggle(activeJSON, false);
            activeToggle.label = "Active";
            activeToggle.backgroundColor = Color.cyan;
            activeToggle.labelText.fontStyle = FontStyle.Bold;
            CreateScrollablePopup(_screensManager.screensJSON).popupPanelHeight = 700f;

            SuperController.singleton.StartCoroutine(DeferredInit());
        }
        catch (Exception)
        {
            enabledJSON.val = false;
            if (_modules != null) Destroy(_modules);
            throw;
        }
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();
        if (!_restored)
        {
            containingAtom.RestoreFromLast(this);
            _restored = true;
        }
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        _screensManager.Show(MainScreen.ScreenName);
    }

    private T CreateModule<T>() where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = false;
        module.plugin = this;
        module.activeJSON = activeJSON;
        return module;
    }

    public void OnDisable()
    {
        activeJSON.val = false;
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
        foreach (var c in _modules.GetComponents<EmbodyModuleBase>())
        {
            var jc = new JSONClass();
            c.StoreJSON(jc);
            json[c.storeId] = jc;
        }

        _screensManager.screensJSON.StoreJSON(json);
        needsStore = true;
        return json;
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        foreach(var c in _modules.GetComponents<EmbodyModuleBase>())
            c.RestoreFromJSON(jc[c.storeId].AsObject);
        _screensManager.screensJSON.RestoreFromJSON(jc);
        _restored = true;
    }
}
