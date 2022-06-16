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
    JSONStorableBool activateOnLoadJSON { get; }
    JSONStorableStringChooser returnToSpawnPoint { get; }
    UIDynamicToggle activeToggle { get; }
    EmbodyTriggerHandler activateTrigger { get; }
    JSONStorableStringChooser presetsJSON { get; }
    void ActivateManually();
    void ActivateForced();
    void Deactivate();
    void Refresh();
    void RefreshDelayed();
    void LoadFromDefaults();
    void StoreJSON(JSONClass json, bool toProfile, bool toScene);
    bool RestoreFromJSONInternal(JSONClass jc, bool fromProfile, bool fromScene);
}

public class Embody : MVRScript, IEmbody
{
    private const string _returnToClosestSpawnPointValue = "(Closest)";

    private static readonly string[] _returnToSpawnPointInitialValues = new[] {"", _returnToClosestSpawnPointValue};

    public JSONStorableBool activeJSON { get; private set; }
    public JSONStorableBool activateOnLoadJSON { get; private set; }
    public JSONStorableStringChooser returnToSpawnPoint { get; private set; }
    public UIDynamicToggle activeToggle { get; private set; }

    public EmbodyTriggerHandler activateTrigger { get; private set; }
    public JSONStorableStringChooser presetsJSON { get; private set; }

    private JSONStorableUrl _loadProfileWithPathUrlJSON;
    private JSONStorableActionPresetFilePath _loadProfileWithPathJSON;
    private GameObject _modules;
    private readonly List<IEmbodyModule> _modulesList = new List<IEmbodyModule>();
    private ScreensManager _screensManager;
    private EmbodyContext _context;
    private bool _restored;
    private bool _activateAfterSaveComplete;
    private EmbodyScaleChangeReceiver _scaleChangeReceiver;
    private NavigationRigSnapshot _navigationRigSnapshot;
    private Coroutine _restoreNavigationRigCoroutine;
    private readonly List<IEmbodyModule> _enabledModules = new List<IEmbodyModule>();
    private JSONArray _poseJSON;
    private bool? _restoreLeftHandEnabled;
    private bool? _restoreRightHandEnabled;
    private bool _activatedManually;
    private bool _active;
    private bool _storablesReferencesInitialized;

    public override void Init()
    {
        try
        {
            if (containingAtom.type == "SessionPluginManager" || containingAtom.type == "CoreControl")
            {
                SuperController.LogError("Embody: Cannot run as a session or scene plugin");
                CreateTextField(new JSONStorableString("Error", "Embody cannot run as a session or scene plugin. Please add it to a Person atom, or an Empty."));
                enabled = false;
                return;
            }

            activeJSON = new JSONStorableBool("Active", false) {isStorable = false};
            activateOnLoadJSON = new JSONStorableBool("ActivateOnLoad", false) {isStorable = true};
            RegisterBool(activateOnLoadJSON);
            returnToSpawnPoint = new JSONStorableStringChooser("ReturnToSpawnPoint", new List<string>(), "", "Return To Spawn Point")
            {
                popupOpenCallback = SyncSpawnPointAtoms
            };
            SyncSpawnPointAtoms();
            RegisterStringChooser(returnToSpawnPoint);

            var isPerson = containingAtom.type == "Person";

            _scaleChangeReceiver = gameObject.AddComponent<EmbodyScaleChangeReceiver>();

            _modules = new GameObject();
            _modules.transform.SetParent(transform, false);
            _modules.SetActive(false);

            _context = new EmbodyContext(this, this);

             var diagnosticsModule = CreateModule<DiagnosticsModule>(_context);
             var automationModule = CreateModule<AutomationModule>(_context);
             var worldScaleModule = CreateModule<WorldScaleModule>(_context);
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

            activateTrigger = new EmbodyTriggerHandler();
            SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;

            _modules.SetActive(true);

            presetsJSON = InitPresets();
            RegisterStringChooser(presetsJSON);

            _context.automation.enabledJSON.val = true;

            _screensManager = new ScreensManager();
            _screensManager.Add(MainScreen.ScreenName, new MainScreen(_context, _modulesList));
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
                _screensManager.Add(ProfilesScreen.ScreenName, new ProfilesScreen(_context));
                _screensManager.Add(MoreScreen.ScreenName, new MoreScreen(_context));
                _screensManager.Add(DiagnosticsScreen.ScreenName, new DiagnosticsScreen(_context, diagnosticsModule));
            }
            else
            {
                _screensManager.Add(PassengerSettingsScreen.ScreenName, new PassengerSettingsScreen(_context, passengerModule));
                _screensManager.Add(WorldScaleSettingsScreen.ScreenName, new WorldScaleSettingsScreen(_context, worldScaleModule));
                _screensManager.Add(MoreScreen.ScreenName, new MoreScreen(_context));
            }

            activeJSON.setCallbackFunction = val =>
            {
                if (val)
                    Activate(false);
                else
                    Deactivate();
            };
            RegisterBool(activeJSON);

            activeToggle = CreateToggle(activeJSON, false);
            activeToggle.label = "Active";
            activeToggle.backgroundColor = Color.cyan;
            activeToggle.labelText.fontStyle = FontStyle.Bold;
            activeToggle.toggle.onValueChanged.AddListener(v =>
            {
                if (!v) return;
                _activatedManually = true;
            });

            _loadProfileWithPathUrlJSON = new JSONStorableUrl("ProfilePathUrl", string.Empty, url =>
            {
                _loadProfileWithPathUrlJSON.valNoCallback = null;
                new Storage(_context).LoadProfile(url);
            }, SaveFormat.SaveExt, SaveFormat.SaveFolder, true)
            {
                allowFullComputerBrowse = false,
                allowBrowseAboveSuggestedPath = true,
                hideExtension = false,
                showDirs = true,
                isRestorable = false,
                isStorable = false,
                beginBrowseWithObjectCallback = jsurl =>
                {
                    FileManagerSecure.CreateDirectory(SaveFormat.SaveFolder);
                    jsurl.shortCuts = FileManagerSecure.GetShortCutsForDirectory(SaveFormat.SaveFolder, false, false, true, true);
                }
            };
            RegisterUrl(_loadProfileWithPathUrlJSON);
            _loadProfileWithPathJSON = new JSONStorableActionPresetFilePath("LoadProfileWithPath", url =>
            {
                _loadProfileWithPathUrlJSON.SetFilePath(url);
            }, _loadProfileWithPathUrlJSON);
            RegisterPresetFilePathAction(_loadProfileWithPathJSON);

            var launchWizardJSON = new JSONStorableAction("LaunchWizard", () => StartCoroutine(LaunchWizard()));
            RegisterAction(launchWizardJSON);

            RegisterAction(new JSONStorableAction("ToggleActive", () => activeJSON.val = !activeJSON.val));
            RegisterAction(new JSONStorableAction("ReinitializeIfActive", () => StartCoroutine(ReinitializeIfActiveCo())));
            RegisterAction(new JSONStorableAction("Activate_Possession", () => ActivatePreset("Improved Possession")));
            RegisterAction(new JSONStorableAction("Activate_LeapMotion", () => ActivatePreset("Improved Possession w/ Leap")));
            RegisterAction(new JSONStorableAction("Activate_Passenger", () => ActivatePreset("Passenger")));

            LoadFromDefaults();

            if (containingAtom.on)
                TryInitializeModuleReferences();

            SuperController.singleton.StartCoroutine(DeferredInit());

            StartCoroutine(activateTrigger.LoadUIAssets());
        }
        catch (Exception)
        {
            enabledJSON.val = false;
            if (_modules != null) Destroy(_modules);
            throw;
        }
    }

    private void ActivatePreset(string presetVal)
    {
        var navigationRigSnapshot = _navigationRigSnapshot;
        if (activeJSON.val)
        {
            Deactivate(false);
        }

        presetsJSON.val = presetVal;

        ActivateManually();

        _navigationRigSnapshot = navigationRigSnapshot;
    }

    public void ActivateManually()
    {
        Activate(true);
    }

    public void ActivateForced()
    {
        Activate(true, true);
    }

    private void Activate(bool activatedManually, bool force = false)
    {
        if (_active)
        {
            return;
        }

        if (!enabled || !containingAtom.on || (!force && activeToggle != null && !activeToggle.toggle.interactable))
        {
            activeJSON.valNoCallback = false;
            return;
        }

        try
        {
            foreach (var atom in SuperController.singleton.GetAtoms())
            {
                foreach (var storableId in atom.GetStorableIDs())
                {
                    if (!storableId.EndsWith("Embody")) continue;
                    var storable = atom.GetStorableByID(storableId);
                    if (storable == null) continue;
                    if (storable == this) continue;
                    storable.SendMessage(nameof(EmbodyDeactivateImmediate), SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        catch (Exception e)
        {
            SuperController.LogError($"Embody: Failed deactivating other instances of Embody. {e}");
        }

        if (!TryInitializeModuleReferences())
        {
            activeJSON.valNoCallback = false;
            return;
        }

        _active = true;
        activeJSON.valNoCallback = true;
        _activatedManually = activatedManually;

        SuperController.singleton.ClearPossess();

        if ((_context.trackers?.selectedJSON?.val ?? false) || (_context.passenger?.selectedJSON?.val ?? false))
        {
            _restoreLeftHandEnabled = SuperController.singleton.commonHandModelControl.leftHandEnabled;
            SuperController.singleton.commonHandModelControl.leftHandEnabled = false;
            _restoreRightHandEnabled = SuperController.singleton.commonHandModelControl.rightHandEnabled;
            SuperController.singleton.commonHandModelControl.rightHandEnabled = false;
        }

        _enabledModules.Clear();

        foreach (var module in _modulesList)
        {
            if (module.skipChangeEnabledWhenActive) continue;
            if (!module.selectedJSON.val)
            {
                module.enabledJSON.val = false;
                continue;
            }

            if (module.Validate())
                _enabledModules.Add(module);
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

        try
        {
            activateTrigger.trigger.active = true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: An error was thrown by a trigger: {exc}");
        }

        if ((_context.trackers?.selectedJSON?.val ?? false) && _context.trackers?.restorePoseAfterPossessJSON.val == true)
        {
            _poseJSON = containingAtom.GetStorableIDs()
                .Select(s => _context.containingAtom.GetStorableByID(s))
                .Where(t => !t.exclude && t.gameObject.activeInHierarchy)
                .Where(t => t is FreeControllerV3 || t is DAZBone)
                .Select(t => t.GetJSON())
                .Aggregate(new JSONArray(), (arrayJSON, storableJSON) =>
                {
                    arrayJSON.Add(storableJSON);
                    return arrayJSON;
                });
        }

        foreach (var module in _enabledModules)
        {
            module.PreActivate();
        }

        foreach (var module in _enabledModules)
        {
            module.enabledJSON.val = true;
        }

        if (_context.automation?.autoArmForRecord.val ?? false)
            Utilities.MarkForRecord(_context);
    }

    private bool TryInitializeModuleReferences()
    {
        if (_storablesReferencesInitialized) return true;
        if (!containingAtom.on) return false;

        try
        {
            _context.InitReferences();
            foreach (var module in _modulesList)
            {
                module.InitReferences();
            }
            _storablesReferencesInitialized = true;

            RegisterBool(_context.leftHandToggle);
            RegisterBool(_context.rightHandToggle);

            return true;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Failed to initialize embody: {exc}");
            return false;
        }
    }

    public void Deactivate()
    {
        Deactivate(true);
    }

    private void Deactivate(bool asyncNavigationRigRestore)
    {
        if (!_active) return;
        _active = false;

        var wasActivatedManually = _activatedManually;
        _activatedManually = false;
        activeJSON.valNoCallback = false;

        _context.automation.Reset();

        foreach (var module in _modulesList.AsEnumerable().Reverse())
        {
            if (module.skipChangeEnabledWhenActive) continue;
            module.enabledJSON.val = false;
        }

        foreach (var module in _enabledModules)
        {
            module.PostDeactivate();
        }

        _enabledModules.Clear();

        if (_poseJSON != null && _context.trackers?.restorePoseAfterPossessJSON.val == true)
        {
            foreach (var storableJSON in _poseJSON.Childs)
            {
                var storableId = storableJSON["id"].Value;
                if (string.IsNullOrEmpty(storableId)) continue;
                var storable = _context.containingAtom.GetStorableByID(storableId);
                if (storable == null) continue;
                var controller = storable as FreeControllerV3;
                // NOTE: We use startedPossess to notify users of this (e.g. Timeline) that this is not a recordable movement
                if (controller != null) controller.startedPossess = true;
                storable.PreRestore();
                storable.RestoreFromJSON(storableJSON.AsObject);
                storable.PostRestore();
                if (controller != null) controller.startedPossess = false;
            }
            _poseJSON = null;
        }

        if (_restoreLeftHandEnabled != null)
        {
            SuperController.singleton.commonHandModelControl.leftHandEnabled = _restoreLeftHandEnabled.Value;
            _restoreLeftHandEnabled = null;
        }
        if (_restoreRightHandEnabled != null)
        {
            SuperController.singleton.commonHandModelControl.rightHandEnabled = _restoreRightHandEnabled.Value;
            _restoreRightHandEnabled = null;
        }

        if (!asyncNavigationRigRestore)
        {
            _navigationRigSnapshot?.Restore();
            _navigationRigSnapshot = null;
            return;
        }

        JSONStorableAction spawnAction;
        if (!wasActivatedManually && TryGetSpawnPoint(out spawnAction))
        {
            _navigationRigSnapshot?.Restore();
            _navigationRigSnapshot = null;
            if (spawnAction == null) throw new NullReferenceException("Null spawn action");
            spawnAction.actionCallback.Invoke();
            _restoreNavigationRigCoroutine = StartCoroutine(CallNextFrame(spawnAction.actionCallback.Invoke));
            return;
        }

        if (_navigationRigSnapshot != null)
        {
            _navigationRigSnapshot?.Restore();
            _restoreNavigationRigCoroutine = StartCoroutine(CallNextFrame(() =>
            {
                _navigationRigSnapshot?.Restore();
                _navigationRigSnapshot = null;
                if (_restoreNavigationRigCoroutine != null)
                {
                    StopCoroutine(_restoreNavigationRigCoroutine);
                    _restoreNavigationRigCoroutine = null;
                }
            }));
        }

        try
        {
            activateTrigger.trigger.active = false;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: An error was thrown by a trigger: {exc}");
        }
    }

    private IEnumerator ReinitializeIfActiveCo()
    {
        if (!activeJSON.val)
        {
            yield break;
        }

        var previousRigSnapshot = _navigationRigSnapshot;
        var restorePoseAfterPossess = _context.trackers.restorePoseAfterPossessJSON.val;
        _context.trackers.restorePoseAfterPossessJSON.val = false;
        Deactivate(false);
        _context.trackers.restorePoseAfterPossessJSON.val = restorePoseAfterPossess;
        yield return 0;
        ActivateManually();
        _navigationRigSnapshot = previousRigSnapshot;
    }

    public void EmbodyDeactivateImmediate()
    {
        if (!activeJSON.val) return;
        Deactivate(false);
    }

    public void Refresh()
    {
        if (!activeJSON.val) return;

        foreach (var module in _enabledModules)
        {
            module.enabledJSON.val = false;
        }

        foreach (var module in _enabledModules)
        {
            module.enabledJSON.val = true;
        }
    }

    public void RefreshDelayed()
    {
        StartCoroutine(RefreshDelayedCo());
    }

    private IEnumerator RefreshDelayedCo()
    {
        if (!activeJSON.val) yield break;

        foreach (var module in _enabledModules)
        {
            module.enabledJSON.val = false;
        }

        yield return 0;
        yield return 0;

        foreach (var module in _enabledModules)
        {
            module.enabledJSON.val = true;
        }
    }

    private IEnumerator CallNextFrame(Action act)
    {
        yield return 0;
        if (this == null) yield break;
        act();
    }

    private IEnumerator DeferredInit()
    {
        yield return new WaitForEndOfFrame();

        if (this == null) yield break;

        var restoredFromLast = false;
        if (!_restored)
        {
            containingAtom.RestoreFromLast(this);
            restoredFromLast = true;
        }

        SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
        _screensManager.Show(MainScreen.ScreenName);

        if (Input.GetKey(KeyCode.LeftControl))
            _context.diagnostics.enabledJSON.val = true;

        if (activateOnLoadJSON.val && !restoredFromLast)
        {
            if (this != null && enabled)
                Activate(false);
        }
    }

    public void LoadFromDefaults()
    {
        try
        {
            if (!FileManagerSecure.FileExists(SaveFormat.DefaultsPath)) return;
            var profile = LoadJSON(SaveFormat.DefaultsPath)?.AsObject;
            if (profile == null) return;
            RestoreFromJSONInternal(profile, true, false);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"Embody: Error loading defaults file: {exc}");
        }
    }

    public override void InitUI()
    {
        base.InitUI();
        if (UITransform == null) return;
        _screensManager?.Init(this, MainScreen.ScreenName);
        activateTrigger.trigger.triggerActionsParent = UITransform;
    }

    public void OnBindingsListRequested(List<object> bindings)
    {
        bindings.Add(new Dictionary<string, string>
        {
            {"Namespace", "Embody"}
        });
        bindings.Add(new JSONStorableAction("ToggleActive", () => { if(activeJSON.val) Deactivate(); else ActivateManually(); }));
        bindings.Add(new JSONStorableAction("Activate", ActivateManually));
        bindings.Add(new JSONStorableAction("Deactivate", Deactivate));
        bindings.Add(new JSONStorableAction("OpenUI", SelectAndOpenUI));
        bindings.Add(new JSONStorableAction("Add_Mirror", () => StartCoroutine(Utilities.CreateMirrorCo(_context.eyeTarget, containingAtom))));
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
        bindings.Add(new JSONStorableAction("ToggleHands", () =>
        {
            var areHandsEnabled = _context.trackers.leftHandMotionControl.enabled || _context.trackers.rightHandMotionControl.enabled;
            _context.trackers.leftHandMotionControl.enabled = !areHandsEnabled;
            _context.trackers.rightHandMotionControl.enabled = !areHandsEnabled;
            _context.trackers.RefreshHands();
            _context.snug.RefreshHands();
        }));
        bindings.Add(new JSONStorableAction("AlignHeadToCamera", AlignHeadToCamera));
        bindings.Add(new JSONStorableAction("MoveEyeTargetToCameraRaycastHit", MoveEyeTargetToCameraRaycastHit));
    }

    private T CreateModule<T>(EmbodyContext context) where T : MonoBehaviour, IEmbodyModule
    {
        var module = _modules.AddComponent<T>();
        module.enabled = false;
        module.context = context;
        module.InitStorables();
        module.activeJSON = activeJSON;
        _modulesList.Add(module);
        return module;
    }

    private void SelectAndOpenUI()
    {
        #if (VAM_GT_1_20)
        SuperController.singleton.SelectController(containingAtom.mainController, false, false, true);
        #else
        SuperController.singleton.SelectController(containingAtom.mainController);
        #endif
        SuperController.singleton.ShowMainHUDAuto();
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
        SuperController.singleton.ShowMainHUDAuto();
        var waitForUI = WaitForUI();
        while (waitForUI.MoveNext())
            yield return waitForUI.Current;
        _screensManager.Show(WizardScreen.ScreenName);
    }

    private JSONStorableStringChooser InitPresets()
    {
        var jss = new JSONStorableStringChooser("Presets", new List<string>
        {
            "Improved Possession",
            "Improved Possession w/ Leap",
            "Snug",
            "Snug Hands Only",
            "Passenger",
            "Passenger w/ Hands",
            "Passenger w/ Leap",
            "Passenger (Free Look)",
            "Leap Fingers Only",
            "Puppeteer (Head)",
            "Hide Head",
            "Legacy Possession",
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
        if (_context.containingAtom.type != "Person") return;

        foreach (var module in _modulesList)
        {
            if (module.skipChangeEnabledWhenActive) continue;
            module.selectedJSON.val = false;
        }

        _context.trackers.headMotionControl.enabled = true;
        _context.trackers.leftHandMotionControl.enabled = true;
        _context.trackers.rightHandMotionControl.enabled = true;
        _context.trackers.leftHandMotionControl.useLeapPositioning = false;
        _context.trackers.rightHandMotionControl.useLeapPositioning = false;

        switch (val)
        {
            case "Legacy Possession":
                _context.hideGeometry.selectedJSON.val = true;
                _context.offsetCamera.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Improved Possession":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, false, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Improved Possession w/ Leap":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, true, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Snug":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, false, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.snug.selectedJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                _context.eyeTarget.selectedJSON.val = true;
                break;
            case "Snug Hands Only":
                _context.trackers.selectedJSON.val = true;
                _context.trackers.headMotionControl.enabled = false;
                ConfigureHandsPresets(true, true, false, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.snug.selectedJSON.val = true;
                break;
            case "Passenger":
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.passenger.rotationLockJSON.val = true;
                _context.passenger.positionLockJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Passenger w/ Hands":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, false, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.passenger.rotationLockJSON.val = true;
                _context.passenger.positionLockJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Passenger w/ Leap":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, true, true);
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.passenger.rotationLockJSON.val = true;
                _context.passenger.positionLockJSON.val = true;
                _context.worldScale.selectedJSON.val = true;
                break;
            case "Passenger (Free Look)":
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.passenger.rotationLockJSON.val = false;
                _context.passenger.positionLockJSON.val = true;
                break;
            case "Passenger (Free Move)":
                _context.hideGeometry.selectedJSON.val = true;
                _context.passenger.selectedJSON.val = true;
                _context.passenger.rotationLockJSON.val = false;
                _context.passenger.positionLockJSON.val = false;
                break;
            case "Leap Only":
                _context.trackers.selectedJSON.val = true;
                _context.trackers.headMotionControl.enabled = false;
                ConfigureHandsPresets(true, false, false, true);
                break;
            case "Puppeteer (Head)":
                _context.trackers.selectedJSON.val = true;
                ConfigureHandsPresets(true, true, false, false);
                _context.trackers.headMotionControl.mappedControllerName = null;
                _context.trackers.leftHandMotionControl.mappedControllerName = null;
                _context.trackers.rightHandMotionControl.mappedControllerName = VamConstants.HeadControlName;
                break;
            case "Hide Head":
                _context.hideGeometry.selectedJSON.val = true;
                _context.hideGeometry.hideFaceJSON.val = true;
                break;
        }

        _context.RefreshTriggers();
        _context.Refresh();
    }

    private void ConfigureHandsPresets(bool trackerEnabled, bool position, bool leap, bool fingers)
    {
        _context.trackers.leftHandMotionControl.enabled = trackerEnabled;
        _context.trackers.leftHandMotionControl.controlPosition = position;
        _context.trackers.leftHandMotionControl.controlRotation = position;
        _context.trackers.leftHandMotionControl.useLeapPositioning = leap;
        _context.trackers.leftHandMotionControl.fingersTracking = fingers;
        _context.trackers.rightHandMotionControl.enabled = trackerEnabled;
        _context.trackers.rightHandMotionControl.controlPosition = position;
        _context.trackers.rightHandMotionControl.controlRotation = position;
        _context.trackers.rightHandMotionControl.useLeapPositioning = leap;
        _context.trackers.rightHandMotionControl.fingersTracking = fingers;
    }

    public void AlignHeadToCamera()
    {
        var cameraTransform = SuperController.singleton.centerCameraTarget.transform;
        var controller = _context.containingAtom.type == "Person"
            ? _context.containingAtom.freeControllers.First(fc => fc.name == "headControl")
            : _context.containingAtom.mainController;
        controller.control.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
    }

    public void MoveEyeTargetToCameraRaycastHit()
    {
        if (_context.containingAtom.type != "Person")
        {
            SuperController.LogError($"Embody: Cannot {nameof(MoveEyeTargetToCameraRaycastHit)} on an atom of type {_context.containingAtom.type}; only Person atoms are supported.");
            return;
        }

        Utilities.MoveToCameraRaycastHit(_context.containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl").control);
    }

    private bool TryGetSpawnPoint(out JSONStorableAction spawnAction)
    {
        if (string.IsNullOrEmpty(returnToSpawnPoint.val))
        {
            spawnAction = null;
            return false;
        }
        return returnToSpawnPoint.val == _returnToClosestSpawnPointValue
            ? TryGetClosestSpawnPoint(out spawnAction)
            : TryGetSpecificSpawnPoint(out spawnAction);
    }

    private bool TryGetClosestSpawnPoint(out JSONStorableAction spawnAction)
    {
        Atom closestSpawnPoint = null;
        var closestDistance = float.MaxValue;
        var cameraPosition = SuperController.singleton.centerCameraTarget.transform.position;
        foreach (var spawnPointAtom in GetSpawnPointAtoms())
        {
            var distance = Mathf.Abs(Vector3.SqrMagnitude(cameraPosition - spawnPointAtom.mainController.control.position));
            if (!(distance < closestDistance)) continue;
            closestDistance = distance;
            closestSpawnPoint = spawnPointAtom;
        }

        if (closestSpawnPoint == null)
        {
            SuperController.LogError($"Embody '{containingAtom.uid}' could not find any SpawnPoint in the scene to return to");
            spawnAction = null;
            return false;
        }

        spawnAction = closestSpawnPoint
            .GetStorableIDs()
            .Select(id => closestSpawnPoint.GetStorableByID(id))
            .Select(s => s.GetAction("Spawn Now"))
            .FirstOrDefault(s => s != null);

        if (spawnAction == null)
        {
            SuperController.LogError($"Embody '{containingAtom.uid}' cannot find an action storable in '{closestSpawnPoint.uid}' named 'Spawn Now'");
            return false;
        }

        return true;
    }

    private bool TryGetSpecificSpawnPoint(out JSONStorableAction spawnAction)
    {
        var spawnPointAtom = SuperController.singleton.GetAtoms().FirstOrDefault(a => a.uid == returnToSpawnPoint.val);
        if (spawnPointAtom == null)
        {
            SuperController.LogError($"Embody '{containingAtom.uid}' cannot restore to SpawnPoint '{returnToSpawnPoint.val}' because this atom does not exist in the scene");
            spawnAction = null;
            return false;
        }

        spawnAction = spawnPointAtom
            .GetStorableIDs()
            .Select(id => spawnPointAtom.GetStorableByID(id))
            .Select(s => s.GetAction("Spawn Now"))
            .FirstOrDefault(act => act != null);
        if (spawnAction != null) return true;

        SuperController.LogError($"Embody '{containingAtom.uid}' cannot find an action storable in '{spawnPointAtom.uid}' named 'Spawn Now'");
        return false;
    }

    private static IEnumerable<Atom> GetSpawnPointAtoms()
    {
        return SuperController.singleton.GetAtoms().Where(a => !ReferenceEquals(a.GetBoolJSONParam("IsSpawnPointHost"), null));
    }

    private void SyncSpawnPointAtoms()
    {
        returnToSpawnPoint.choices = _returnToSpawnPointInitialValues.Concat(GetSpawnPointAtoms().Select(a => a.uid)).ToList();
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
        if (activeJSON != null) Deactivate();
        if (_context?.wizard != null)
            _context.wizard.StopWizard("The wizard was canceled because Embody was disabled.");
    }

    // ReSharper disable once UnusedMember.Local
    private void OnBeforeSceneSave()
    {
        if (activeJSON.val)
        {
            _activateAfterSaveComplete = true;
            var navigationRigSnapshot = _navigationRigSnapshot;
            var activatedManually = _activatedManually;
            Deactivate(false);
            _navigationRigSnapshot = navigationRigSnapshot;
            _activatedManually = activatedManually;
        }
    }

    // ReSharper disable once UnusedMember.Local
    private void OnSceneSaved()
    {
        if (_activateAfterSaveComplete)
        {
            _activateAfterSaveComplete = false;
            var navigationRigSnapshot = _navigationRigSnapshot;
            Activate(_activatedManually);
            _navigationRigSnapshot = navigationRigSnapshot;
        }
    }

    public void OnDestroy()
    {
        SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRename;
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
        StoreJSON(json, false, true);
        needsStore = true;
        return json;
    }

    public void StoreJSON(JSONClass json, bool toProfile, bool toScene)
    {
        json["Version"].AsInt = SaveFormat.Version;
        foreach (var c in _modulesList)
        {
            var jc = new JSONClass();
            c.StoreJSON(jc, toProfile || _context.diagnostics.enabledJSON.val, toScene);
            json[c.storeId] = jc;
        }

        if (toScene)
            json["Triggers"] = activateTrigger.trigger.GetJSON();
    }

    public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
        base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
        RestoreFromJSONInternal(jc, false, true);
        _restored = true;
    }

    public bool RestoreFromJSONInternal(JSONClass jc, bool fromProfile, bool fromScene)
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
            SuperController.LogError("Embody: This scene was saved with a more recent Embody version than the one you have installed. Please get the latest version.");
            return false;
        }

        foreach (var c in _modulesList)
            c.RestoreFromJSON(jc[c.storeId].AsObject, fromProfile, fromScene);

        _context.RefreshTriggers();

        if (jc.HasKey("Triggers"))
            activateTrigger.trigger.RestoreFromJSON(jc["Triggers"].AsObject);

        return false;
    }

    #region Triggers

    public override void Validate()
    {
        base.Validate();
        activateTrigger?.trigger?.Validate();
    }

    private void OnAtomRename(string from, string to)
    {
        activateTrigger?.trigger.SyncAtomNames();
    }

    #endregion
}
