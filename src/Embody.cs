using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

public interface IEmbody
{
    JSONStorableBool activeJSON { get; }
    JSONStorableStringChooser presetsJSON { get; }
}

public class Embody : MVRScript, IEmbody
{
    public JSONStorableBool activeJSON { get; private set; }
    public JSONStorableStringChooser presetsJSON { get; private set; }

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
            var isPerson = containingAtom.type == "Person";

            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            _context = new EmbodyContext(this, this);
            _context.Initialize();

             var automationModule = CreateModule<AutomationModule>(_context);
             var worldScaleModule = isPerson ? CreateModule<WorldScaleModule>(_context) : null;
             var hideGeometryModule = isPerson ? CreateModule<HideGeometryModule>(_context) : null;
             var offsetCameraModule = isPerson ? CreateModule<OffsetCameraModule>(_context) : null;
             var passengerModule = CreateModule<PassengerModule>(_context);
             var trackersModule = isPerson ? CreateModule<TrackersModule>(_context) : null;
             var snugModule = isPerson ? CreateModule<SnugModule>(_context) : null;
             var eyeTargetModule = isPerson ? CreateModule<EyeTargetModule>(_context) : null;
             var wizardModule = isPerson ? CreateModule<WizardModule>(_context) : null;

            _context.automation = automationModule;
            _context.worldScale = worldScaleModule;
            _context.hideGeometry = hideGeometryModule;
            _context.offsetCamera = offsetCameraModule;
            _context.passenger = passengerModule;
            _context.trackers = trackersModule;
            _context.snug = snugModule;
            _context.eyeTarget = eyeTargetModule;
            _context.wizard = wizardModule;

            _modules.SetActive(true);

            var modules = _modules.GetComponents<IEmbodyModule>();
            presetsJSON = InitPresets(modules);
            RegisterStringChooser(presetsJSON);

            _context.automation.enabledJSON.val = true;

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(_context, modules));
            if (isPerson)
            {
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
            }
            else
            {
                _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(_context, passengerModule));
            }

            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                {
                    if (!enabled)
                    {
                        activeJSON.valNoCallback = false;
                        return;
                    }

                    _context.Initialize();
                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        if (module.skipChangeEnabledWhenActive) continue;
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
                        if (module.skipChangeEnabledWhenActive) continue;
                        module.enabledJSON.val = false;
                    }
                }
            };
            RegisterBool(activeJSON);

            var activeToggle = CreateToggle(activeJSON, false);
            activeToggle.label = "Active";
            activeToggle.backgroundColor = Color.cyan;
            activeToggle.labelText.fontStyle = FontStyle.Bold;

            var launchWizardJSON = new JSONStorableAction("LaunchWizard", () => StartCoroutine(LaunchWizard()));
            RegisterAction(launchWizardJSON);

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
                RestoreFromJSON(profile.AsObject, false, false, null, false);
            }
        }

        SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        _screensManager?.Init(this, MainScreen.ScreenName);
    }

    public void OnBindingsListRequested(List<object> bindings)
    {
        bindings.Add(new Dictionary<string, string>
        {
            {"Namespace", "Embody"}
        });
        bindings.Add(new JSONStorableAction("ToggleActive", () => activeJSON.val = !activeJSON.val));
        bindings.Add(new JSONStorableAction("OpenUI", SelectAndOpenUI));
        bindings.Add(new JSONStorableAction("SpawnMirror", () => StartCoroutine(Utilities.CreateMirror(_context.eyeTarget, containingAtom))));
    }

    private T CreateModule<T>(EmbodyContext context) where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = false;
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
        SuperController.singleton.ShowMainHUDMonitor();
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
            if (UITransform == null) SuperController.LogError("Embody: No UI");
            UITransform.gameObject.SetActive(true);
            yield break;
        }
    }

    private IEnumerator LaunchWizard()
    {
        SuperController.singleton.SelectController(containingAtom.mainController);
        SuperController.singleton.ShowMainHUDMonitor();
        var waitForUI = WaitForUI();
        while (waitForUI.MoveNext())
            yield return waitForUI.Current;
        _screensManager.Show(WizardScreen.ScreenName);
    }

    private JSONStorableStringChooser InitPresets(IEmbodyModule[] modules)
    {
        return new JSONStorableStringChooser("Presets", new List<string>
        {
            "VaM Possession",
            "Improved Possession",
            "Snug",
            "Passenger",
        }, "(Select to apply)", "Apply preset", (string val) =>
        {
            foreach (var module in modules)
            {
                if (module.skipChangeEnabledWhenActive) continue;
                module.selectedJSON.val = false;
            }

            switch (val)
            {
                case "VaM Possession":
                    _context.automation.takeOverVamPossess.val = false;
                    _context.hideGeometry.selectedJSON.val = true;
                    _context.offsetCamera.selectedJSON.val = true;
                    break;
                case "Improved Possession":
                    _context.automation.takeOverVamPossess.val = true;
                    _context.trackers.selectedJSON.val = true;
                    _context.hideGeometry.selectedJSON.val = true;
                    _context.worldScale.selectedJSON.val = true;
                    _context.eyeTarget.selectedJSON.val = true;
                    break;
                case "Snug":
                    _context.automation.takeOverVamPossess.val = true;
                    _context.trackers.selectedJSON.val = true;
                    _context.hideGeometry.selectedJSON.val = true;
                    _context.snug.selectedJSON.val = true;
                    _context.worldScale.selectedJSON.val = true;
                    _context.eyeTarget.selectedJSON.val = true;
                    break;
                case "Passenger":
                    _context.automation.takeOverVamPossess.val = true;
                    _context.hideGeometry.selectedJSON.val = true;
                    _context.passenger.selectedJSON.val = true;
                    _context.worldScale.selectedJSON.val = true;
                    _context.eyeTarget.selectedJSON.val = true;
                    break;
            }
        });
    }

    public void OnEnable()
    {
#if(VAM_GT_1_20_77_0)
        SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSave;
        SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;
#endif
    }

    public void OnDisable()
    {
#if(VAM_GT_1_20_77_0)
        SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSave;
        SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
#endif
        activeJSON.val = false;
        _modules.GetComponent<WizardModule>()?.StopWizard("");
    }

    // ReSharper disable once UnusedMember.Local
    private void OnBeforeSceneSave()
    {
        if (activeJSON.val)
        {
            _activateAfterSaveComplete = true;
            activeJSON.val = false;
        }

        _modules.GetComponent<WizardModule>()?.StopWizard("");
    }

    // ReSharper disable once UnusedMember.Local
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
        _context.wizard.StopWizard("");
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
        if (version < 2)
        {
            SuperController.LogError("Embody: Saved settings are not compatible with this version of Embody.");
            _restored = true;
            return;
        }
        if (version > SaveFormat.Version)
        {
            SuperController.LogError("Embody: This scene was saved with a more recent Embody version than the one you have install. Please get the latest version.");
        }
        foreach(var c in _modules.GetComponents<EmbodyModuleBase>())
            c.RestoreFromJSON(jc[c.storeId].AsObject);
        _restored = true;
    }
}
