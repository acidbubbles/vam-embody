using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public interface IWizard : IEmbodyModule
{
    WizardStatusChangedEvent statusChanged { get; }
    JSONStorableString statusJSON { get; }
    JSONStorableBool experimentalSnugWizardJSON { get; }
    bool isRunning { get; }
    JSONStorableBool experimentalViveTrackersWizardJSON { get; }
    void StartWizard();
    void StopWizard(string message = null);
    void Next();
    void Skip();
}

public class WizardStatusChangedEvent : UnityEvent<bool> { }

public class WizardModule : EmbodyModuleBase, IWizard
{
    private static readonly string _wizardIntroMessage = (@"
This wizard will <b>walk you through</b> the main Embody features, and help you <b>configure</b> them so you can make the most out of this plugin.

Before starting:

- Try <b>activating Embody with the
  default settings</b>, it might work
  fine without further adjustments!
- You can <b>adjust your hands</b> in the
  <i>Configure Trackers</i> menu if they
  don't feel right.
- Try looking around, you can tweak the
  head center (<b>offset</b>) in the
  <i>Configure Trackers</i> menu.

Keep in mind that:

- <b>Press A on your controller</b> to go to
  the next step; you don't have to use
  the Next button.
- The wizard <i>cannot be closed</i> and
  other atoms cannot be selected until
  you <i>Stop</i> the wizard.

When you are ready, select <b>Start Wizard</b>.").TrimStart();
    private const string _noHandsMessage = "Cannot start the wizard. No hand trackers were found. Are you running in Desktop mode?";

    public const string Label = "Wizard";
    public override string storeId => "Wizard";
    public override string label => Label;
    public override bool skipChangeEnabledWhenActive => true;

    public WizardStatusChangedEvent statusChanged { get; } = new WizardStatusChangedEvent();
    public JSONStorableString statusJSON { get; } = new JSONStorableString("WizardStatus", "") {isStorable = false};
    public JSONStorableBool experimentalSnugWizardJSON { get; } = new JSONStorableBool("ExperimentalSnugWizard", false);
    public JSONStorableBool experimentalViveTrackersWizardJSON { get; } = new JSONStorableBool("ExperimentalViveTrackersWizard", false);
    public bool isRunning => _coroutine != null;
    public bool forceReopen { get; set; } = true;

    private Coroutine _coroutine;
    private IWizardStep _step;
    private bool _next;
    private bool _skip;
    private JSONArray _poseJSON;
    private NavigationRigSnapshot _navigationRigSnapshot;

    public override void InitStorables()
    {
        base.InitStorables();

        statusJSON.val = _wizardIntroMessage;
    }

    public void StartWizard()
    {
        if (_coroutine != null) StopWizard();
        if (context.LeftHand() == null || context.RightHand() == null)
        {
            statusJSON.val = _noHandsMessage;
            return;
        }

        context.diagnostics.Log("Wizard: Start");

        _navigationRigSnapshot = NavigationRigSnapshot.Snap();

        _poseJSON = containingAtom.GetStorableIDs()
            .Select(s => containingAtom.GetStorableByID(s))
            .Where(t => !t.exclude && t.gameObject.activeInHierarchy)
            .Where(t => t is FreeControllerV3 || t is DAZBone)
            .Select(t => t.GetJSON())
            .Aggregate(new JSONArray(), (arrayJSON, storableJSON) =>
            {
                arrayJSON.Add(storableJSON);
                return arrayJSON;
            });

        enabledJSON.val = true;
        _coroutine = StartCoroutine(WizardCoroutine());
        statusChanged.Invoke(true);
    }

    public void StopWizard(string message = null)
    {
        if (!enabledJSON.val) return;

        context.diagnostics.Log("Wizard: Stop");

        StopCoroutine(_coroutine);
        _coroutine = null;

        if (_step != null)
        {
            try
            {
                _step.Leave(true);
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Leave failed: {exc}");
            }
            _step = null;
        }

        context.embody.Deactivate();
        context.trackers.previewTrackerOffsetJSON.val = false;
        context.snug.previewSnugOffsetJSON.val = false;

        statusJSON.val = message ?? _wizardIntroMessage;
        _next = false;
        _skip = false;
        statusChanged.Invoke(false);
        enabledJSON.val = false;
        SuperController.singleton.worldScale = 1f;

        if (_poseJSON != null)
        {
            foreach (var storableJSON in _poseJSON.Childs)
            {
                var storableId = storableJSON["id"].Value;
                if (string.IsNullOrEmpty(storableId)) continue;
                var storable = containingAtom.GetStorableByID(storableId);
                storable.PreRestore();
                storable.RestoreFromJSON(storableJSON.AsObject);
                storable.PostRestore();
            }
            _poseJSON = null;
        }

        if (_navigationRigSnapshot != null)
        {
            _navigationRigSnapshot.Restore();
            _navigationRigSnapshot = null;
        }
    }

    public void Next()
    {
        _next = true;
    }

    public void Skip()
    {
        _skip = true;
    }

    private IEnumerator WizardCoroutine()
    {
        context.diagnostics.TakeSnapshot("Start Wizard");

        context.embody.Deactivate();

        yield return 0;

        SuperController.singleton.worldScale = 1f;

        context.embody.presetsJSON.val = "Improved Possession";

        var steps = BuildSteps();
        var error = false;

        for (var i = 0; i < steps.Count; i++)
        {
            _step = steps[i];
            if (error)
            {
                statusJSON.val = $"ERROR: {_step.lastError ?? "[No error message]"} / {steps.Count}\n\n{_step.helpText}";
                error = false;
                _step.lastError = null;
            }
            else
            {
                statusJSON.val = $"Step {i + 1} / {steps.Count}\n\n{_step.helpText}";
            }

            // NOTE: Not strictly necessary, but allows physics to settle and ensures final leave will be invoked
            yield return new WaitForSecondsRealtime(0.2f);

            try
            {
                context.diagnostics.Log($"Wizard: Enter {_step}");
                _step.Enter();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Enter failed: {exc}");
                StopWizard("An error prevented the wizard from finishing");
                yield break;
            }

            while (!AreAnyStartRecordKeysDown())
            {
                ReopenIfClosed();

                if (_skip)
                    break;

                yield return 0;

                try
                {
                    _step.Update();
                }
                catch (Exception exc)
                {
                    SuperController.LogError($"Embody: Wizard {_step}.Update failed: {exc}");
                    StopWizard("An error prevented the wizard from finishing");
                    yield break;
                }
            }

            ReopenIfClosed();

            try
            {
                if (_skip)
                {
                    _skip = false;
                }
                else
                {
                    if (!_step.Apply())
                    {
                        i--;
                        error = true;
                    }
                }
            }
            catch (Exception exc)
            {
                SuperController.LogError($"Embody: Wizard {_step}.Apply failed: {exc}");
                StopWizard("An error prevented the wizard from finishing");
                yield break;
            }
            finally
            {
                try
                {
                    _step?.Leave(false);
                }
                catch (Exception exc)
                {
                    SuperController.LogError($"Embody: Wizard {_step}.Leave failed: {exc}");
                    StopWizard("An error prevented the wizard from finishing");
                }
            }
        }

        StopWizard("<b>All done!</b>\n\nYou can now activate Embody.\n\nYou can tweak your settings manually by pressing Back, or start this wizard again if you prefer.\n\nYou can save tweaks in your default profile in the <i>Import, Export & Default Settings</i> screen. Default settings will automatically apply whenever you load this plugin on an atom.\n\nNow, have fun living in the future!");
    }

    private void ReopenIfClosed()
    {
        if (!forceReopen) return;
        if (context.plugin.UITransform.gameObject.activeInHierarchy) return;
        if (!SuperController.singleton.mainHUD.gameObject.activeInHierarchy) return;
        SuperController.singleton.SelectController(containingAtom.mainController, false, false, false);
        if(XRDevice.isPresent)
            SuperController.singleton.ShowMainHUD();
        else
            SuperController.singleton.ShowMainHUDMonitor();
        var selector = containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
        selector.SetActiveTab("Plugins");
        context.plugin.UITransform.gameObject.SetActive(true);
    }

    private List<IWizardStep> BuildSteps()
    {
        var steps = new List<IWizardStep>();

        // NOTE: We use Count because we want to sync all available motion controls, not only the first one
        var useViveTrackers = context.trackers.viveTrackers.Count(t => t.SyncMotionControl());

        steps.Add(new ResetSettingsStep(context));
        steps.Add(new ResetPoseStep(context));
        steps.Add(new RecordPlayerHeightStep(context));
        if (useViveTrackers > 0)
        {
            if (experimentalViveTrackersWizardJSON.val)
                steps.Add(new ExperimentalAskViveTrackersStep(context, steps));
            else
                steps.Add(new RecordViveTrackersStep(context));
        }

        if (experimentalSnugWizardJSON.val)
        {
            if (context.LeftHand() != null && context.RightHand() != null)
                steps.Add(new AskSnugStep(context, steps));
        }
        steps.Add(new MakeDefaultsStep(context));

        return steps;
    }

    private bool AreAnyStartRecordKeysDown()
    {
        if (_next || _skip)
        {
            _next = false;
            return true;
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            _skip = true;
            return true;
        }
        var sc = SuperController.singleton;
        if (sc.isOVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.Touch)) return true;
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.Touch)) return true;
        }
        if (sc.isOpenVR)
        {
            if (sc.selectAction.stateDown) return true;
        }
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        return false;
    }

    public override void StoreJSON(JSONClass jc, bool toProfile, bool toScene)
    {
        base.StoreJSON(jc, toProfile, toScene);

        if (toProfile)
        {
            experimentalSnugWizardJSON.StoreJSON(jc);
            experimentalViveTrackersWizardJSON.StoreJSON(jc);
        }
    }

    public override void RestoreFromJSON(JSONClass jc, bool fromProfile, bool fromScene)
    {
        base.RestoreFromJSON(jc, fromProfile, fromScene);

        if (fromProfile)
        {
            experimentalSnugWizardJSON.RestoreFromJSON(jc);
            experimentalViveTrackersWizardJSON.RestoreFromJSON(jc);
        }
    }

    public override void ResetToDefault()
    {
        base.ResetToDefault();

        experimentalSnugWizardJSON.SetValToDefault();
        experimentalViveTrackersWizardJSON.SetValToDefault();
    }
}
