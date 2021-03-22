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
    UIDynamicToggle activeToggle { get; }
    JSONStorableStringChooser presetsJSON { get; }
    void LoadFromDefaults();
    void StoreJSON(JSONClass json, bool includeProfile);
    bool RestoreFromJSONInternal(JSONClass jc, bool fromDefaults);
}

public class Embody : MVRScript, IEmbody
{
    public JSONStorableBool activeJSON { get; private set; }
    public UIDynamicToggle activeToggle { get; private set; }
    public JSONStorableStringChooser presetsJSON { get; private set; }

    private GameObject _modules;
    private ScreensManager _screensManager;
    private EmbodyContext _context;
    private bool _restored;
    private bool _activateAfterSaveComplete;
    private EmbodyScaleChangeReceiver _scaleChangeReceiver;
    private NavigationRigSnapshot _navigationRigSnapshot;
    private Coroutine _restoreNavigationRigCoroutine;

    public override void Init()
    {
        try
        {
            activeJSON = new JSONStorableBool("Active", false) {isStorable = false};
            var isPerson = containingAtom.type == "Person";

            _scaleChangeReceiver = gameObject.AddComponent<EmbodyScaleChangeReceiver>();

            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            _context = new EmbodyContext(this, this);

             var diagnosticsModule = CreateModule<DiagnosticsModule>(_context);
             var automationModule = CreateModule<AutomationModule>(_context);
             var worldScaleModule = isPerson ? CreateModule<WorldScaleModule>(_context) : null;
             var hideGeometryModule = isPerson ? CreateModule<HideGeometryModule>(_context) : null;
             var offsetCameraModule = isPerson ? CreateModule<OffsetCameraModule>(_context) : null;
             var passengerModule = CreateModule<PassengerModule>(_context);
             var trackersModule = isPerson ? CreateModule<TrackersModule>(_context) : null;
             var snugModule = isPerson ? CreateModule<SnugModule>(_context) : null;
             var eyeTargetModule = isPerson ? CreateModule<EyeTargetModule>(_context) : null;
             var wizardModule = isPerson ? CreateModule<WizardModule>(_context) : null;

            _context.diagnostics = diagnosticsModule;
            _context.automation = automationModule;
            _context.worldScale = worldScaleModule;
            _context.hideGeometry = hideGeometryModule;
            _context.offsetCamera = offsetCameraModule;
            _context.passenger = passengerModule;
            _context.trackers = trackersModule;
            _context.snug = snugModule;
            _context.eyeTarget = eyeTargetModule;
            _context.wizard = wizardModule;

            if (_scaleChangeReceiver != null)
            {
                _context.scaleChangeReceiver = _scaleChangeReceiver;
                _scaleChangeReceiver.context = _context;
                containingAtom.RegisterDynamicScaleChangeReceiver(_scaleChangeReceiver);
            }

            _modules.SetActive(true);

            presetsJSON = InitPresets();
            RegisterStringChooser(presetsJSON);

            _context.automation.enabledJSON.val = true;

            _screensManager = new ScreensManager();
            var modules = _modules.GetComponents<IEmbodyModule>();
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
                _screensManager.Add(WizardScreen.ScreenName, new WizardScreen(_context, wizardModule));
                _screensManager.Add(ImportExportScreen.ScreenName, new ImportExportScreen(_context, this, worldScaleModule, snugModule));
                _screensManager.Add(MoreScreen.ScreenName, new MoreScreen(_context));
                _screensManager.Add(DiagnosticsScreen.ScreenName, new DiagnosticsScreen(_context, diagnosticsModule));
            }
            else
            {
                _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(_context, passengerModule));
            }

            var modulesToEnable = new List<IEmbodyModule>();
            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                {
                    if (!enabled)
                    {
                        activeJSON.valNoCallback = false;
                        return;
                    }

                    modulesToEnable.Clear();

                    foreach (var module in _modules.GetComponents<IEmbodyModule>())
                    {
                        if (module.skipChangeEnabledWhenActive) continue;
                        if (!module.selectedJSON.val)
                        {
                            module.enabledJSON.val = false;
                            continue;
                        }
                        if (module.BeforeEnable())
                            modulesToEnable.Add(module);
                    }

                    if (_restoreNavigationRigCoroutine != null)
                    {
                        StopCoroutine(_restoreNavigationRigCoroutine);
                        _restoreNavigationRigCoroutine = null;
                    }
                    if (_navigationRigSnapshot == null && ((_context.trackers?.selectedJSON?.val ?? false) || (_context.passenger?.selectedJSON?.val ?? false)))
                    {
                        _navigationRigSnapshot = NavigationRigSnapshot.Snap();
                    }

                    foreach (var module in modulesToEnable)
                    {
                        module.enabledJSON.val = true;
                    }

                    modulesToEnable.Clear();

                    if(_context.automation?.autoArmForRecord.val ?? false)
                        Utilities.MarkForRecord(_context);
                }
                else
                {
                    automationModule.Reset();
                    foreach (var module in _modules.GetComponents<IEmbodyModule>().Reverse())
                    {
                        if (module.skipChangeEnabledWhenActive) continue;
                        module.enabledJSON.val = false;
                    }

                    _navigationRigSnapshot?.Restore();
                    _restoreNavigationRigCoroutine = StartCoroutine(RestoreNavigationRig());
                }
            };
            RegisterBool(activeJSON);

            activeToggle = CreateToggle(activeJSON, false);
            activeToggle.label = "Active";
            activeToggle.backgroundColor = Color.cyan;
            activeToggle.labelText.fontStyle = FontStyle.Bold;

            var launchWizardJSON = new JSONStorableAction("LaunchWizard", () => StartCoroutine(LaunchWizard()));
            RegisterAction(launchWizardJSON);

            RegisterAction(new JSONStorableAction("ToggleActive", () => activeJSON.val = !activeJSON.val));

            SuperController.singleton.StartCoroutine(DeferredInit());
        }
        catch (Exception)
        {
            enabledJSON.val = false;
            if (_modules != null) Destroy(_modules);
            throw;
        }
    }

    private IEnumerator RestoreNavigationRig()
    {
        yield return 0;
        _navigationRigSnapshot?.Restore();
        _navigationRigSnapshot = null;
        _restoreNavigationRigCoroutine = null;
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();
        if (!_restored)
        {
            containingAtom.RestoreFromLast(this);
        }
        LoadFromDefaults();
        _restored = true;

        SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        _screensManager.Show(MainScreen.ScreenName);

        if (Input.GetKey(KeyCode.LeftControl))
        {
            _context.diagnostics.enabledJSON.val = true;
        }
    }

    public void LoadFromDefaults()
    {
        if (!FileManagerSecure.FileExists(SaveFormat.DefaultsPath)) return;
        var profile = LoadJSON(SaveFormat.DefaultsPath)?.AsObject;
        if (profile == null) return;
        RestoreFromJSONInternal(profile, _restored);
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
        bindings.Add(new JSONStorableAction("ToggleActive", () => { if (activeToggle != null && activeToggle.toggle.interactable) { activeJSON.val = !activeJSON.val; } }));
        bindings.Add(new JSONStorableAction("Activate", () => { if (activeToggle != null && activeToggle.toggle.interactable) { activeJSON.val = true; } }));
        bindings.Add(new JSONStorableAction("Deactivate", () => { if (activeToggle != null && activeToggle.toggle.interactable) { activeJSON.val = false; } }));
        bindings.Add(new JSONStorableAction("OpenUI", SelectAndOpenUI));
        bindings.Add(new JSONStorableAction("SpawnMirror", () => StartCoroutine(Utilities.CreateMirror(_context.eyeTarget, containingAtom))));
        bindings.Add(new JSONStorableAction("Preset_Passenger", () => SelectPreset("Passenger")));
        bindings.Add(new JSONStorableAction("Preset_Snug", () => SelectPreset("Snug")));
        bindings.Add(new JSONStorableAction("Preset_ImprovedPossession", () => SelectPreset("Improved Possession")));
        bindings.Add(new JSONStorableAction("Enable_Leap", () =>
        {
            _context.trackers.leftHandMotionControl.fingersTracking = true;
            _context.trackers.leftHandMotionControl.fingersTracking = true;
            _context.trackers.leftHandMotionControl.useLeapPositioning = true;
            _context.trackers.leftHandMotionControl.useLeapPositioning = true;
            SuperController.singleton.disableLeap = false;
        }));
        bindings.Add(new JSONStorableAction("Disable_Leap", () =>
        {
            _context.trackers.leftHandMotionControl.fingersTracking = true;
            _context.trackers.leftHandMotionControl.fingersTracking = true;
            _context.trackers.leftHandMotionControl.useLeapPositioning = false;
            _context.trackers.leftHandMotionControl.useLeapPositioning = false;
            SuperController.singleton.disableLeap = true;
        }));
        bindings.Add(new JSONStorableAction("StartRecord", () => Utilities.StartRecord(_context)));
        bindings.Add(new JSONStorableAction("ApplyPossessionPose", () => new PossessionPose(_context).Apply()));
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

    private JSONStorableStringChooser InitPresets()
    {
        var jss = new JSONStorableStringChooser("Presets", new List<string>
        {
            "VaM Possession",
            "Improved Possession",
            "Improved Possession w/ Leap",
            "Snug",
            "Passenger",
            "Passenger w/ Hands",
            "Passenger w/ Leap",
        }, "(Select To Apply)", "Apply Preset")
        {
            isStorable = false
        };
        jss.setCallbackFunction = val =>
        {
            SelectPreset(val);
            jss.valNoCallback = $"({val} Applied)";
        };
        return jss;
    }

    private void SelectPreset(string val)
    {
        var modules = _modules.GetComponents<IEmbodyModule>();
        foreach (var module in modules)
        {
            if (module.skipChangeEnabledWhenActive) continue;
            module.selectedJSON.val = false;
        }

        _context.trackers.leftHandMotionControl.useLeapPositioning = false;
        _context.trackers.rightHandMotionControl.useLeapPositioning = false;

        switch (val)
        {
            case "VaM Possession":
                _context.hideGeometry.selectedJSON.val = true;
                _context.offsetCamera.selectedJSON.val = true;
                break;
            case "Improved Possession":
                _context.trackers.selectedJSON.val = true;
                _context.hideGeometry.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Improved Possession w/ Leap":
                _context.trackers.selectedJSON.val = true;
                _context.trackers.leftHandMotionControl.useLeapPositioning = true;
                _context.trackers.rightHandMotionControl.useLeapPositioning = true;
                _context.hideGeometry.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Snug":
                _context.trackers.selectedJSON.val = true;
                _context.hideGeometry.selectedJSON.val = true;
                _context.snug.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Passenger":
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Passenger w/ Hands":
                _context.trackers.selectedJSON.val = true;
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Passenger w/ Leap":
                _context.trackers.selectedJSON.val = true;
                _context.trackers.leftHandMotionControl.useLeapPositioning = true;
                _context.trackers.rightHandMotionControl.useLeapPositioning = true;
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
        }
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
        _modules.GetComponent<WizardModule>()?.StopWizard("The wizard was canceled because Embody was disabled.");
    }

    // ReSharper disable once UnusedMember.Local
    private void OnBeforeSceneSave()
    {
        if (activeJSON.val)
        {
            _activateAfterSaveComplete = true;
            activeJSON.val = false;
        }
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
        if (_context?.containingAtom != null)
            _context.containingAtom.DeregisterDynamicScaleChangeReceiver(_scaleChangeReceiver);
        Destroy(_scaleChangeReceiver);
        Destroy(_modules);
        foreach(var cue in VisualCuesHelper.Cues)
            Destroy(cue);
        SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
        var json = base.GetJSON(includePhysical, includeAppearance, forceStore);
        StoreJSON(json, false);
        needsStore = true;
        return json;
    }

    public void StoreJSON(JSONClass json, bool includeProfile)
    {
        json["Version"].AsInt = SaveFormat.Version;
        foreach (var c in _modules.GetComponents<EmbodyModuleBase>())
        {
            var jc = new JSONClass();
            c.StoreJSON(jc, includeProfile || _context.diagnostics.enabledJSON.val);
            json[c.storeId] = jc;
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        if (RestoreFromJSONInternal(jc, false))
            _restored = true;
    }

    public bool RestoreFromJSONInternal(JSONClass jc, bool fromDefaults)
    {
        var version = jc["Version"].AsInt;
        if (version <= 0) return false;
        if (version < 2)
        {
            SuperController.LogError($"Embody: Saved settings (Save Format {version}) are not compatible with this version of Embody (Save Format {SaveFormat.Version}).");
            return false;
        }
        if (version > SaveFormat.Version)
        {
            SuperController.LogError("Embody: This scene was saved with a more recent Embody version than the one you have install. Please get the latest version.");
            return false;
        }

        foreach (var c in _modules.GetComponents<EmbodyModuleBase>())
            c.RestoreFromJSON(jc[c.storeId].AsObject, fromDefaults);

        return false;
    }
}
