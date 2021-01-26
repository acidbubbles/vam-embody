using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using MVR.FileManagementSecure;
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
    private EmbodyContext _context;
    private bool _restored;
    private bool _activateAfterSaveComplete;

    public override void Init()
    {
        try
        {
            activeJSON = new JSONStorableBool("Active", false) {isStorable = false};

            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            _context = new EmbodyContext(this, this);
            _context.Initialize();

            var automationModule = CreateModule<AutomationModule>(_context);
            var worldScaleModule = CreateModule<WorldScaleModule>(_context);
            var hideGeometryModule = CreateModule<HideGeometryModule>(_context);
            var offsetCameraModule = CreateModule<OffsetCameraModule>(_context);
            var passengerModule = CreateModule<PassengerModule>(_context);
            var trackersModule = CreateModule<TrackersModule>(_context);
            var snugModule = CreateModule<SnugModule>(_context);
            var eyeTargetModule = CreateModule<EyeTargetModule>(_context);
            var wizardModule = CreateModule<WizardModule>(_context);

            automationModule.embody = this;
            trackersModule.snug = snugModule;
            trackersModule.passenger = passengerModule;
            snugModule.trackers = trackersModule;
            wizardModule.embody = this;
            wizardModule.passenger = passengerModule;
            wizardModule.worldScale = worldScaleModule;
            wizardModule.snug = snugModule;

            _modules.SetActive(true);

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(_context, _modules.GetComponents<IEmbodyModule>(), wizardModule));
            _screensManager.Add(TrackersSettingsScreen.ScreenName, new TrackersSettingsScreen(_context, trackersModule));
            _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(_context, passengerModule));
            _screensManager.Add(SnugSettingsScreen.ScreenName, new SnugSettingsScreen(_context, snugModule));
            _screensManager.Add(HideGeometrySettingsScreen.ScreenName, new HideGeometrySettingsScreen(_context, hideGeometryModule));
            _screensManager.Add(OffsetCameraSettingsScreen.ScreenName, new OffsetCameraSettingsScreen(_context, offsetCameraModule));
            _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(_context, worldScaleModule));
            _screensManager.Add(EyeTargetSettingsScreen.ScreenName, new EyeTargetSettingsScreen(_context, eyeTargetModule));
            _screensManager.Add(AutomationSettingsScreen.ScreenName, new AutomationSettingsScreen(_context, automationModule));
            _screensManager.Add(WizardScreen.ScreenName, new WizardScreen(_context, wizardModule));
            _screensManager.Add(ImportExportScreen.ScreenName, new ImportExportScreen(_context, this, worldScaleModule, snugModule));
            _screensManager.Add(UtilitiesScreen.ScreenName, new UtilitiesScreen(_context));

            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                {
                    _context.Initialize();
                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        if (module.alwaysEnabled) continue;
                        if (!module.selectedJSON.val)
                        {
                            module.enabledJSON.val = false;
                            continue;
                        }
                        if (module.BeforeEnable())
                            module.enabledJSON.val = true;
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
            var popup = CreateScrollablePopup(_screensManager.screensJSON);
            popup.popupPanelHeight = 700f;
            popup.AddNav(this);

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
        }
        if (!_restored)
        {
            if(FileManagerSecure.FileExists(SaveFormat.DefaultsPath))
            {
                var profile = LoadJSON(SaveFormat.DefaultsPath);
                SuperController.LogMessage(profile.ToString());
            }
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
        bindings.Add(new JSONStorableAction("SpawnMirror", () => StartCoroutine(Utilities.CreateMirror(containingAtom))));
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

    public void OnEnable()
    {
#if(VAM_GT_1_20_6_0)
        SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
        SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;
#endif
    }

    public void OnDisable()
    {
#if(VAM_GT_1_20_6_0)
        SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
        SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
#endif
        activeJSON.val = false;
    }

    private void OnBeforeSceneSave()
    {
        if (activeJSON.val)
        {
            _activateAfterSaveComplete = true;
            activeJSON.val = false;
        }
    }

    private void OnSceneSaved()
    {
        if (_activateAfterSaveComplete)
        {
            _activateAfterSaveComplete = false;
            activeJSON.val = true;
        }
    }

    public void OnDestroy()
    {
        Destroy(_modules);
        foreach(var cue in VisualCuesHelper.Cues)
            Destroy(cue);
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
        json["Version"].AsInt = SaveFormat.Version;
        foreach (var c in _modules.GetComponents<EmbodyModuleBase>())
        {
            var jc = new JSONClass();
            c.StoreJSON(jc);
            json[c.storeId] = jc;
        }
        needsStore = true;
        return json;
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        var version = jc["Version"].AsInt;
        if (version <= 0) return;
        if (version > SaveFormat.Version)
        {
            SuperController.LogError("Embody: This scene was saved with a more recent Embody version than the one you have install. Please get the latest version.");
        }
        foreach(var c in _modules.GetComponents<EmbodyModuleBase>())
            c.RestoreFromJSON(jc[c.storeId].AsObject);
        _restored = true;
    }
}
