using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            var context = new EmbodyContext(this);
            context.Initialize();

            var automationModule = CreateModule<AutomationModule>(context);
            automationModule.embody = this;
            var worldScaleModule = CreateModule<WorldScaleModule>(context);
            var trackersModule = CreateModule<TrackersModule>(context);
            var hideGeometryModule = CreateModule<HideGeometryModule>(context);
            var offsetCameraModule = CreateModule<OffsetCameraModule>(context);
            var passengerModule = CreateModule<PassengerModule>(context);
            var snugModule = CreateModule<SnugModule>(context);
            var eyeTargetModule = CreateModule<EyeTargetModule>(context);
            var wizardModule = CreateModule<EmbodyWizardModule>(context);
            wizardModule.worldScale = worldScaleModule;
            wizardModule.snug = snugModule;
            wizardModule.trackers = trackersModule;

            // TODO: Once awaken, register the useful storables so they can be modified by scripts

            _modules.SetActive(true);

            // TODO: This is weird structure wise. Review architecture so those modules have their place.
            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(context, _modules.GetComponents<IEmbodyModule>(), wizardModule));
            _screensManager.Add(TrackersSettingsScreen.ScreenName, new TrackersSettingsScreen(context, trackersModule));
            _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(context, passengerModule));
            _screensManager.Add(SnugSettingsScreen.ScreenName, new SnugSettingsScreen(context, snugModule));
            _screensManager.Add(HideGeometrySettingsScreen.ScreenName, new HideGeometrySettingsScreen(context, hideGeometryModule));
            _screensManager.Add(OffsetCameraSettingsScreen.ScreenName, new OffsetCameraSettingsScreen(context, offsetCameraModule));
            _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(context, worldScaleModule));
            _screensManager.Add(EyeTargetSettingsScreen.ScreenName, new EyeTargetSettingsScreen(context, eyeTargetModule));
            _screensManager.Add(AutomationSettingsScreen.ScreenName, new AutomationSettingsScreen(context, automationModule));
            _screensManager.Add(EmbodyWizardSettingsScreen.ScreenName, new EmbodyWizardSettingsScreen(context, wizardModule));
            _screensManager.Add(ImportExportScreen.ScreenName, new ImportExportScreen(context, this));

            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                {
                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        if (module.alwaysEnabled) continue;
                        module.enabledJSON.val = module.selectedJSON.val;
                    }
                }
                else
                {
                    automationModule.Reset();
                    foreach (var module in _modules.GetComponents<IEmbodyModule>().Reverse())
                    {
                        if (module.alwaysEnabled) continue;
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
        SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        _screensManager.Show(MainScreen.ScreenName);
    }

    public void OnBindingsListRequested(List<object> bindings)
    {
        bindings.Add(new Dictionary<string, string>
        {
            {"Namespace", "Embody"}
        });
        bindings.Add(new JSONStorableAction("ToggleActive", () => activeJSON.val = !activeJSON.val));
        bindings.Add(new JSONStorableAction("OpenUI", SelectAndOpenUI));
    }

    private T CreateModule<T>(EmbodyContext context) where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = module.alwaysEnabled;
        module.context = context;
        module.activeJSON = activeJSON;
        return module;
    }

    private void SelectAndOpenUI()
    {
        #if (VAM_GT_1_20)
        SuperController.singleton.SelectController(containingAtom.mainController, false, false, true);
        #else
        SuperController.singleton.SelectController(containingAtom.mainController);
        #endif
        StartCoroutine(WaitForUI());
    }

    private IEnumerator WaitForUI()
    {
        var expiration = Time.unscaledTime + 1f;
        while (Time.unscaledTime < expiration)
        {
            yield return 0;
            var selector = containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
            if(selector == null) continue;
            selector.SetActiveTab("Plugins");
            if (UITransform == null) SuperController.LogError("Timeline: No UI");
            UITransform.gameObject.SetActive(true);
            yield break;
        }
    }

    public void OnDisable()
    {
        activeJSON.val = false;
    }

    public void OnDestroy()
    {
        Destroy(_modules);
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
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
